@echo Running ngrok on localhost:3978...
call ngrok http -bind-tls=true --host-header="localhost:80" 3978
@pause