# RPCore 0.10.1 — downloads

- [Atualização 0.10.1 — DLL pronta e instalador](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.10.1.zip)
- [Código-fonte 0.10.1](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.10.1.zip)
- [Correção do pátio LS GAS e teste](docs/CORRECAO-0.10.1.md)

O bloqueio “Patio LS GAS ocupado” usava raios muito amplos e uma única posição. A atualização procura até 35 pares de vagas com verificação de modelos/chão/cenário. Não apaga trânsito, carros pessoais nem objetos. FuelDelivery.json, saves e celular F2 são preservados. Não reinstale OIV/YTD/DLC para esta correção.

Feche o GTA, extraia o ZIP e execute Install-RPCore.ps1 dessa pasta com o caminho real do jogo. F10 → Trabalhos → Transporte de combustivel → GPS/Iniciar contrato. Os veículos podem aparecer em uma vaga próxima: siga os blips. [Instruções de engate, descarga e retorno](docs/FASE-4.2-0.10.0.md). Build sem erros/avisos e 309 testes; spawn e ciclo completo ainda precisam do teste no GTA.

Source contém o csproj; update contém a DLL pronta. ZIPs sem versão apontam para esta entrega.
