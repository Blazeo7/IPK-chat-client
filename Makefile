all: build script

build: 
	dotnet publish

script:
	@echo "#!/bin/bash" > ./ipk24chat-client
	@echo 'dotnet ClientServer/bin/Release/net8.0/ClientServer.dll $$@' >> ./ipk24chat-client
	@chmod +x ./ipk24chat-client

clean:
	dotnet clean

.PHONY: all build script