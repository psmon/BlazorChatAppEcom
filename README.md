# Blazor 그래픽 웹채팅

![ex_screenshot](./doc/intro.png)

Blazor + SigralR + Actor(Akka.net)를 이용한 그래픽 채팅앱입니다.

추가 문서 : https://wiki.webnori.com/display/webfr/BlazorWebChat


# 참고링크 :


Network :
- https://docs.microsoft.com/ko-kr/aspnet/core/tutorials/signalr-blazor?view=aspnetcore-6.0&tabs=visual-studio&pivots=server
- https://wiki.webnori.com/display/webfr/Blazor+With+AKKA
- 

Graphic:
- https://swharden.com/blog/2021-01-07-blazor-canvas-animated-graphics/  
- https://www.davidguida.net/blazor-and-2d-game-development-part-1-intro/
- https://luizmelo.itch.io/medieval-warrior-pack-2
- https://opengameart.org/
- https://products.aspose.app/pdf/ko/split-png

Utils:
- https://codebeautify.org/base64-to-image-converter
- https://www.base64-image.de/

CSS UX :
- https://www.htmlelements.com/blazor/
- https://www.tablesgenerator.com/html_tables
- https://html-cleaner.com/

맵툴:
- https://www.mapeditor.org/
- https://rpg.hamsterrepublic.com/ohrrpgce/Free_Tilemaps


# Blazor StandAloe Docker

## 로컬빌드

    docker builder prune

	docker build -f BlazorChatApp/Server/Dockerfile -t registry.webnori.com/blazor-chatapp-server:dev .

### OnlyBuild
	docker-compose build  -

### Build and UP
	docker-compose up --build

	http://localhost:4080


## 배포

	docker push registry.webnori.com/blazor-chatapp-server:dev

	docker push registry.webnori.com/blazor-chatapp-client:dev

```
version: '2'
services:
  blazor-chat-app:
    image: registry.webnori.com/blazor-chatapp-server:dev
    environment:
      ASPNETCORE_ENVIRONMENT: Development
    ports:
    - 8001:8080/tcp
    labels:
      io.rancher.scheduler.affinity:host_label: server=late01
      io.rancher.container.hostname_override: container_name
      io.rancher.container.pull_image: always

```


	https://chat.webnori.com/


