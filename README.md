# RPCore — GTA V Legacy

Projeto C# .NET Framework 4.8 para **ScriptHookVDotNet Enhanced 1.1.0.6** e **LemonUI SHVDN3 2.2**, as versões informadas pelo desenvolvedor. Implementação própria sobre os fontes enviados, mantendo `ISystem`, LemonUI e os formatos básicos de inventário, necessidades e pizza.

## Instalar no GTA

1. Feche o GTA. Extraia **RPCore-update.zip** em uma pasta temporária, fora de `scripts`.
2. Com ScriptHookV, SHVDN Enhanced e LemonUI já instalados, execute no PowerShell:

   ```powershell
   .\Install-RPCore.ps1 -GtaPath 'D:\Games\Grand Theft Auto V'
   ```

3. O instalador salva a DLL anterior e toda a pasta `scripts/RPCore` em `RPCore-backups`, fora da pasta de scripts. Substitui somente `RPCore.dll`; copia configurações e recursos ausentes sem sobrescrever os existentes. Não substitui SHVDN, LemonUI, arquivos do jogo ou saves existentes. Newtonsoft.Json é instalado apenas se estiver ausente; uma versão incompatível bloqueia a instalação antes das alterações.
4. Inicie o modo história. F10 abre o menu principal. Não mantenha uma cópia renomeada da DLL antiga dentro de `scripts`, pois o SHVDN também a executaria.

Configurações antigas são preservadas: se `Settings.ini` já existir, acrescente as novas opções a partir de `payload/scripts/RPCore/Settings.ini` quando necessário. `Keys.ini`, seção `SETTINGS`, tem prioridade para `Inventory Key` e `Seatbelt Key`. Por isso o cinto usa **Z** no pacote fornecido; numa instalação antiga com outra tecla em `Keys.ini`, essa tecla continua valendo.

## Controles padrão

| Tecla | Ação |
|---|---|
| I | Inventário; selecionar um consumível para usá-lo |
| E | Abrir uma loja quando estiver próximo ao balcão e a pé |
| F6 | Iniciar/cancelar turno de pizza, com 5 entregas por padrão |
| F7 | Conquistas e progresso |
| Z | Colocar/retirar cinto em veículo compatível |
| F8 | Armazenamento, a pé junto ao porta-malas destrancado ou ao baú configurado |
| F5 | Bolsa simulada: depósito, saque, compra/venda e lista de acompanhamento |
| F4 | Gangues: GPS para HQ, afiliação presencial e saída da gangue |
| F3 | Rádio: selecionar/parar uma transmissão |
| F9 | Registrar coordenadas no log |
| F10 | Salvar, banco, configuração, importar e selecionar perfis |

Altere as teclas em `Settings.ini` se conflitam com trainers ou outros mods. Use F10 → Reload configuration; alterações de `Features` precisam de reinício do script.

## Dados e compatibilidade

- `Items.json` e `Shops.json` mantêm os formatos originais. `Inventory/*.json` permite listas adicionais de itens; o último arquivo em ordem alfabética pode substituir um ID do catálogo base. IDs desconhecidos nos saves são preservados.
- `SaveData/<perfil>` mantém necessidades, inventário, pizza, conquistas, carteira e afiliação. `Inventory.json` também contém os compartimentos de armazenamento, para transferir itens e salvar ambos os lados em um arquivo atômico. Há `.bak` da versão anterior. Um JSON que falhou na leitura fica protegido contra gravação automática naquela sessão.
- Saves antigos do RPCore diretamente em `SaveData` são copiados uma única vez para o primeiro perfil detectado. `FlatMigration.json` registra o destino; os originais permanecem intactos. Faça a primeira inicialização com o protagonista dono desses saves.
- Michael, Franklin e Trevor têm perfis separados. Peds freemode usam o perfil selecionado no F10, persistido em `ActiveProfile.json`. Modelos desconhecidos/transições mantêm o último perfil. O RPCore não troca aparência ou modelo automaticamente.
- Arquivos originais do PSRP ficam em `assets/RPCore` nos fontes e em `scripts/RPCore/Legacy` no pacote, com áudio e gangues nos diretórios correspondentes. F10 importa os três jogadores enviados ou saves encontrados em `scripts/PSRP/SaveData/Players`. A importação nunca sobrescreve um perfil existente: cria `PSRP_<nome>` e retém uma cópia byte a byte do JSON completo, com todos os 151 campos.
- Importação funcional: fome, hidratação, dinheiro, banco, gangue/reputação, carteira de ações e conquistas já desbloqueadas. Campos de aparência, veículos, negócios, dívidas e territórios são preservados na cópia original; não são aplicados por esta versão. A importação não entrega dinheiro de um save MP para um protagonista.
- `Wallet.json` é o checkpoint principal de dinheiro MP, banco e bolsa. Os protagonistas usam o dinheiro nativo do GTA; o salvamento desse dinheiro depende também do save do próprio jogo. A carteira em `StockMarket/<perfil>.portfolio.json` é exportada como cópia compatível; a bolsa RPCore é uma simulação local com três ativos e variação por minuto, sem integração com BAWSAQ/LCN.
- Porta-malas são identificados por modelo + placa dentro do perfil. Duas cópias do mesmo modelo com a mesma placa compartilham o compartimento; use placas distintas. O baú padrão está em `(-14.1, -1440.2, 31.1)`, editável em `Storage/Locations.json`. Limites: 100 kg no porta-malas e 200 kg no baú padrão.
- Gangues leem os três registros de `Gangs/CustomGangs.json`: nome, descrição, HQ e cor. Entre a pé no HQ pelo menu; cada entrega de pizza concede reputação até 100. Esta versão não cria NPCs armados, não muda relacionamentos globais nem simula guerras territoriais.
- Rádio usa Windows Media Player COM e conexão de rede no computador do GTA. Instale/habilite esse componente do Windows se necessário e forneça URLs diretas de áudio MP3 em `CustomRadioStream.xml`. O endereço fornecido no PSRP aparenta ser uma página de rádio, não um fluxo MP3 garantido; falhas de conexão são exibidas e registradas. Desligue o rádio nativo manualmente para evitar sobreposição. Reprodução real exige teste no Windows.
- Áudios de cinto, compra, entrega e conquista usam os WAVs PCM enviados, sem conversão dos originais. Os demais WAVs estão preservados. Os dois `RivalDealer*.wav` são IEEE float: não são enviados ao `SoundPlayer`, que espera PCM.
- Não há alterações em polícia, combustível, handling, armas, interior, mapas ou outros mods. Opções antigas de `Keys.ini` e `Legacy/SaveData/Settings.ini` referentes a esses sistemas são preservadas, mas não ganham efeito automaticamente no RPCore.

Veja [o relatório de alterações](docs/ALTERACOES.md) e [os testes no GTA](docs/TESTES-GTA.md). A compilação e os testes de dados executam no Linux; a execução do jogo e do áudio COM exige Windows/GTA.

## Compilar e validar

Linux/cloud, dentro deste checkout isolado (sem criar worktree):

```bash
bash scripts/setup.sh
```

O script instala SDK .NET 8.0.419 em `/workspace/.tools`, baixa as referências oficiais verificando SHA-256, restaura com lockfiles, compila `net48` usando reference assemblies e executa os testes de dados em `net8.0`. Não inicia o GTA nem instala DLLs de runtime no jogo.

Windows com SDK .NET e as DLLs instaladas no GTA:

```powershell
dotnet build RPCore.csproj -c Release -p:GTAV='D:\Games\Grand Theft Auto V'
dotnet test tests/RPCore.Tests.csproj -c Release
python scripts/package.py
```

Se `.deps` existir, ela tem prioridade. Use `-p:ReferenceDir='D:\referencias'` para indicar outra pasta com as duas DLLs de referência. `python scripts/package.py` gera `dist/RPCore-update.zip` e `dist/RPCore-source.zip` após a compilação. Nenhum teste externo afirma ter executado o GTA.
