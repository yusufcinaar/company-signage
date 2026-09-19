"""HTTP regressions. Run only against a disposable local test server/database.

SIGNAGE_TEST_URL and SIGNAGE_TEST_PASSWORD must be supplied by the runner.
Uses Python 3 standard library only.
"""
import os, base64, hashlib, json, uuid, time, re, html
from datetime import datetime, timezone, timedelta
from urllib.parse import urlsplit
from http_client import Session

BASE=os.environ['SIGNAGE_TEST_URL'].rstrip('/')
assert urlsplit(BASE).hostname in ('127.0.0.1','localhost'), 'Disposable local server required'
password=os.environ['SIGNAGE_TEST_PASSWORD']
anon=Session(); admin=Session(); checks=[]

def check(name,ok,detail=''):
    checks.append({'name':name,'passed':bool(ok),'detail':str(detail)})
    print(('PASS' if ok else 'FAIL')+': '+name+' '+str(detail),flush=True)
    if not ok: raise AssertionError(name+': '+str(detail))

def call(method,path,session=anon,**kwargs):
    return session.request(method,BASE+path,allow_redirects=False,**kwargs)

def token():return call('GET','/api/auth/csrf',admin).json()['token']
csrf=token()
r=call('POST','/api/auth/login',admin,json={'username':'admin','password':password},headers={'X-CSRF-TOKEN':csrf})
check('API login creates authenticated session',r.status_code==200 and bool(admin.cookies),r.status_code)
csrf=token()  # Refresh token after the authentication identity changes.
def manage(method,path,**kwargs):
    return call(method,path,admin,headers={'X-CSRF-TOKEN':csrf},**kwargs)

for path in ['/','/Screen','/Media','/Playlist','/Assignment','/Settings','/Log']:
    check('Panel '+path,call('GET',path,admin).status_code==200)
for path in ['/api/screens','/api/media','/api/playlists','/api/assignments']:
    check('Anonymous management blocked '+path,call('GET',path).status_code in (401,403))
check('Anonymous mutation blocked',call('POST','/api/screens',json={}).status_code in (401,403))
check('Authenticated mutation needs CSRF',call('POST','/api/screens',admin,json={}).status_code==400)

prefix='TEST-'+uuid.uuid4().hex[:8].upper()
screens=[]; device_tokens=[]
for index in range(2):
    secret=uuid.uuid4().hex+uuid.uuid4().hex;device_tokens.append(secret)
    r=manage('POST','/api/screens',json={'name':f'Demo ekran {index+1}','screenCode':f'{prefix}-{index}', 'deviceToken':secret,'screenWidth':1920,'screenHeight':1080})
    check('Create authenticated screen '+str(index),r.status_code==201,r.text[:120])
    screens.append(r.json())
sid=screens[0]['id'];code=screens[0]['screenCode']
headers={'X-Screen-Code':code,'X-Device-Token':device_tokens[0]}
def device(method,path,**kwargs):return call(method,path,headers=headers,**kwargs)
publication=f'/api/player/{code}/current-publication'
check('Missing device token rejected',call('GET',publication).status_code==401)
check('Wrong device token rejected',call('GET',publication,headers={**headers,'X-Device-Token':'invalid'}).status_code==401)
check('Cross-screen token rejected',device('GET',f"/api/player/{screens[1]['screenCode']}/configuration").status_code==401)
check('Valid token accepted',device('GET',publication).status_code==200)
check('SignalR negotiation rejects anonymous device',call('POST','/signageHub/negotiate?negotiateVersion=1').status_code==401)
check('SignalR negotiation accepts valid device',device('POST','/signageHub/negotiate?negotiateVersion=1').status_code==200)
heartbeat=f'/api/player/{code}/heartbeat'
check('Anonymous heartbeat rejected',call('POST',heartbeat,json={'screenCode':code}).status_code==401)
check('Valid heartbeat accepted',device('POST',heartbeat,json={'screenCode':code,'ipAddress':'127.0.0.1','playerVersion':'test'}).status_code==200)

png=base64.b64decode('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII=')
media=[]
for i in range(2):
    r=manage('POST','/api/media/upload',data={'name':f'Demo image {i}','displayDuration':'8','soundEnabled':'false'},files={'file':(f'demo-{i}.png',png,'image/png')})
    check('Media upload '+str(i),r.status_code==200,r.status_code);media.append(r.json())
check('Media download requires authentication',call('GET',media[0]['fileUrl']).status_code==401)
download=device('GET',media[0]['fileUrl'])
check('Authorized download bytes',download.content==png)
r=manage('POST','/api/assignments/send-now',json={'screenIds':[sid],'mediaFileId':media[0]['id'],'assignmentType':'SingleMedia'})
check('Send immediate publication',r.status_code==200)
immediate=device('GET',publication).json()
check('Player sees immediate publication',immediate['mediaFileId']==media[0]['id'])
check('Published hash matches downloaded bytes',hashlib.sha256(download.content).hexdigest()==immediate['fileHash'].lower())
def assign(**changes):
    data={'screenId':sid,'mediaFileId':media[1]['id'],'assignmentType':'SingleMedia','priority':200}
    data.update(changes)
    return manage('POST','/api/assignments',json=data)
r=assign(startDate='2099-01-01T00:00:00Z',endDate='2099-01-02T00:00:00Z')
check('Future assignment created',r.status_code==200,r.text[:120])
check('Future publication does not start early',device('GET',publication).json()['mediaFileId']==media[0]['id'])
r=assign(startDate='2000-01-01T00:00:00Z',endDate='2001-01-01T00:00:00Z')
check('Expired publication ignored',r.status_code==200 and device('GET',publication).json()['mediaFileId']==media[0]['id'])
r=assign(startDate='2000-01-01T00:00:00Z',endDate='2098-01-01T00:00:00Z')
check('Active higher priority publication selected',r.status_code==200 and device('GET',publication).json()['mediaFileId']==media[1]['id'])
aid=r.json()['id']
check('Delete active assignment',manage('DELETE',f'/api/assignments/{aid}').status_code==200)
check('Deletion restores previous eligible publication',device('GET',publication).json()['mediaFileId']==media[0]['id'])
check('Invalid date interval rejected',assign(startDate='2099-01-02T00:00:00Z',endDate='2099-01-01T00:00:00Z').status_code==400)

# Real clock boundaries must switch without any further management action.
now=datetime.now(timezone.utc)
start=now+timedelta(seconds=4);end=now+timedelta(seconds=9)
r=assign(startDate=start.isoformat(),endDate=end.isoformat())
check('Timed publication waits',r.status_code==200 and device('GET',publication).json()['mediaFileId']==media[0]['id'])
time.sleep(max(0,(start-datetime.now(timezone.utc)).total_seconds())+0.3)
check('Timed publication starts automatically',device('GET',publication).json()['mediaFileId']==media[1]['id'])
time.sleep(max(0,(end-datetime.now(timezone.utc)).total_seconds())+0.3)
check('Timed publication ends automatically',device('GET',publication).json()['mediaFileId']==media[0]['id'])

# Deleting the last eligible assignment must clear both manifest and screen state.
for assignment in manage('GET','/api/assignments').json():
    if assignment['screenId']==sid:
        check('Delete assignment '+str(assignment['id']),manage('DELETE',f"/api/assignments/{assignment['id']}").status_code==200)
empty=device('GET',publication).json()
check('No stale publication after final deletion',empty.get('mediaFileId') is None and empty.get('playlistId') is None)

r=manage('POST','/api/playlists',json={'name':'Demo playlist','isLoop':True});pid=r.json()['id']
for m in media:
    r=manage('POST',f'/api/playlists/{pid}/items',json={'mediaFileId':m['id'],'displayDuration':8,'soundEnabled':False})
    check('Add playlist item '+str(m['id']),r.status_code==200)
items=r.json()['items']
check('Reorder playlist',manage('PUT',f'/api/playlists/{pid}/reorder',json={'itemIds':[i['id'] for i in reversed(items)]}).status_code==200)
check('Send playlist',manage('POST','/api/assignments/send-now',json={'screenIds':[sid],'playlistId':pid,'assignmentType':'Playlist'}).status_code==200)
pub=device('GET',publication).json()
check('Player sees reordered playlist',pub['playlistId']==pid and [i['mediaFileId'] for i in pub['playlistItems']]==[m['id'] for m in reversed(media)])
check('Anonymous stop rejected',call('POST',f'/api/screens/{sid}/stop').status_code in (401,403))
check('Administrator stop allowed',manage('POST',f'/api/screens/{sid}/stop').status_code==200)
check('Stopped publication stays empty for polling clients',device('GET',publication).json().get('playlistId') is None)
check('Publication restarts when sent again',manage('POST','/api/assignments/send-now',json={'screenIds':[sid],'playlistId':pid,'assignmentType':'Playlist'}).status_code==200)
page=call('GET','/',admin).text
logout_form=re.search(r'<form[^>]*action="/Auth/Logout"[^>]*>(.*?)</form>',page,re.S).group(1)
form_token=html.unescape(re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"',logout_form).group(1))
check('Panel logout form works',call('POST','/Auth/Logout',admin,data={'__RequestVerificationToken':form_token}).status_code==302)
print(f'{len(checks)} HTTP checks passed.')
if os.environ.get('SIGNAGE_TEST_STATE'):
    with open(os.environ['SIGNAGE_TEST_STATE'],'w',encoding='utf-8') as f:
        json.dump({'checks':checks,'screenCode':code,'token':device_tokens[0],'screenId':sid,'otherScreen':screens[1]['screenCode'],'baseUrl':BASE,'mediaId':media[0]['id']},f,indent=2)
