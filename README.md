# Camera Dino

Aplicativo local para monitoramento e retransmissão de vídeo. Ele captura fluxos de câmeras via protocolos de rede (como RTSP e ONVIF) e os converte em tempo real para transmissão e visualização otimizada.

## Como funciona?

O **Camera Dino** atua essencialmente como um **Servidor de Retransmissão de Vídeo (Restreamer/Transcoder)**. Ele utiliza motores poderosos de mídia (o **go2rtc** e o **FFmpeg**) embutidos numa solução local rápida.

Ele funciona nas seguintes etapas:
1. **Captura (Ingress):** Ele se conecta a uma fonte de vídeo (como uma Câmera IP, DVR, NVR) puxando o fluxo original da rede. Geralmente, essas fontes transmitem em protocolos como **RTSP** ou **ONVIF**, que são pesados para navegadores web nativos.
2. **Processamento (Transmuxing):** Rodando em segundo plano, o programa intercepta esses pacotes de rede crus. Em vez de apenas salvar o vídeo, o motor faz uma "tradução simultânea" daquele protocolo, reempacotando o vídeo e o áudio em milissegundos, com latência ultrabaixa.
3. **Transmissão (Egress):** Ele pega esse fluxo processado e o "joga de volta na rede" sob novos protocolos muito mais modernos e acessíveis, como **WebRTC** ou **MSE**. 

Se você tem uma câmera de segurança com link RTSP complicado, o Camera Dino atua como ponte: suga o RTSP e cospe uma interface web leve que qualquer dispositivo da rede (PC, celular, Smart TV) consegue abrir instantaneamente.

## Compilação e Build

Para recompilar o projeto, execute o script PowerShell `build_inno.ps1` na raiz.
Os ícones podem ser gerados pelos scripts `Make-ValidIcon.ps1` e `create_icon.ps1`.

O instalador gerado será salvo na pasta `Release`.
