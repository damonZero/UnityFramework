# KJ Host update test server

Run from this directory:

```powershell
python -m http.server 8080 --directory C:\ZZS\Project\NewProjectK\KJ\Server
```

MuMu reaches the Windows host as `10.0.2.2`.
The APK uses:
`http://10.0.2.2:8080/CDN/Android/DefaultPackage`

The Host publisher writes the active package at that path and keeps a versioned copy below it.
