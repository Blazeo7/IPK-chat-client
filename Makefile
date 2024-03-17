all: build script

build: 
	dotnet build

script:
	@echo "#!/bin/bash" > ./ipk24chat-client
	@echo 'dotnet run --project ./ClientServer/ClientServer.csproj -- $$@'  >> ./ipk24chat-client
	@chmod +x ./ipk24chat-client

clean:
	dotnet clean

.PHONY: all build script