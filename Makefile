all: build script

build: 
	dotnet publish

script:
	@echo "#!/bin/bash" > ./ipk24chat-client
	@echo 'dotnet ChatApp/bin/Release/net8.0/ChatApp.dll $$@' >> ./ipk24chat-client
	@chmod +x ./ipk24chat-client

clean:
	dotnet clean
	rm -f ipk24chat-client

.PHONY: all build script