from pathlib import Path
import json, re, base64, hashlib
import urllib.request, urllib.error, http.cookiejar, uuid

class Reply:
    def __init__(self,r):
        self.status_code=r.code
        self.content=r.read()
        self.text=self.content.decode('utf-8',errors='replace')
    def json(self): return json.loads(self.text)

class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self,*args,**kwargs): return None

class Session:
    def __init__(self): self.jar=http.cookiejar.CookieJar()
    @property
    def cookies(self):return {c.name:c.value for c in self.jar}
    def request(self,method,url,timeout=30,allow_redirects=True,json=None,data=None,files=None,headers=None):
        import json as jsonlib
        headers=dict(headers or {})
        body=None
        if json is not None:
            body=jsonlib.dumps(json).encode();headers['Content-Type']='application/json'
        elif files:
            boundary='audit'+uuid.uuid4().hex
            parts=[]
            for key,value in (data or {}).items():
                parts.append(f'--{boundary}\r\nContent-Disposition: form-data; name="{key}"\r\n\r\n{value}\r\n'.encode())
            for key,(name,content,mime) in files.items():
                parts.append(f'--{boundary}\r\nContent-Disposition: form-data; name="{key}"; filename="{name}"\r\nContent-Type: {mime}\r\n\r\n'.encode()+content+b'\r\n')
            parts.append(f'--{boundary}--\r\n'.encode())
            body=b''.join(parts);headers['Content-Type']='multipart/form-data; boundary='+boundary
        elif data is not None:
            body=urllib.parse.urlencode(data).encode();headers['Content-Type']='application/x-www-form-urlencoded'
        handlers=[urllib.request.HTTPCookieProcessor(self.jar)]
        if not allow_redirects: handlers.append(NoRedirect())
        opener=urllib.request.build_opener(*handlers)
        req=urllib.request.Request(url,data=body,headers=headers,method=method)
        try:return Reply(opener.open(req,timeout=timeout))
        except urllib.error.HTTPError as error:return Reply(error)
    def get(self,url,**kw):return self.request('GET',url,**kw)
    def post(self,url,**kw):return self.request('POST',url,**kw)

class requests:
    Session=Session
    @staticmethod
    def request(method,url,**kw): return Session().request(method,url,**kw)
