# PSRP.dll / YTD — auditoria e RPCore 0.5.1

## Caminho confirmado

O assembly.xml do OIV enviado define update/update.rpf > x64/data/cdimages/scaleform_generic.rpf. Com instalação na pasta mods, o destino é:

```text
<GTA>\mods\update\update.rpf\x64\data\cdimages\scaleform_generic.rpf
```

Esse é o destino mostrado no segundo print do OpenIV. A pasta rpf/update/update.rpf vista dentro do OIV é a organização interna do pacote. Nomes e tamanhos em KiB dos seis PSRP*.ytd do print coincidem com o pacote enviado; isso não é uma verificação de hash dos arquivos instalados no computador do usuário. Hashes dos oito YTDs originais enviados: PSRP-YTD-FILES.json.

A DLL não abre esse caminho com File.Read/Directory. Ela solicita dicionários e imagens ao motor do GTA. A função de streaming resolve o arquivo montado no jogo. O nome usado em C# é, por exemplo, PSRP_Inventory, sem .ytd e sem caminho do Windows; a imagem interna é RadMenu1. O GTA precisa estar carregando o arquivo através do sistema de mods.

## Varredura da DLL

Foi exportado o projeto inteiro com ILSpy 9.1.0.7988 em diretório de referência, fora do RPCore, sem executar a PSRP.dll. A varredura abrange 75 arquivos C#: 251 construções ScaledTexture, 21 TextureAsset, 2 chamadas diretas DRAW_SPRITE e 82 referências de fábricas de slots do radial. Não foram encontradas strings literais de caminhos .ytd/.rpf nesses arquivos C#. O relatório registra os hashes dos fontes exportados, arquivo/linha, nomes, expressões dinâmicas e limites da análise: PSRP-TEXTURE-AUDIT.json. Não é uma demonstração de funcionamento da PSRP no GTA.

A inspeção da LemonUI 2.2 mostra que ScaledTexture.Request chama HAS_STREAMED_TEXTURE_DICT_LOADED e REQUEST_STREAMED_TEXTURE_DICT(Dictionary, true). Draw chama DRAW_SPRITE(Dictionary, Texture, posição, tamanho, cor). O RPCore agora usa o mesmo argumento de requisição e o mesmo mecanismo do motor. Não há carregador de PNG externo nem extração de YTD no jogo.

| Tela/recurso | Dicionário do GTA | Imagens internas confirmadas |
|---|---|---|
| HUD/veículo | PSRP | HealthIcon, ArmorIcon, DrinkVisual, FoodIcon, LungIcon, SleepIcon, OutBar0..100, MoneyUI, BankUI, CarFuelIcon, CarSeatbeltIcon, CarCruiseIcon etc. |
| Inventário | PSRP_Inventory | Background, InventoryBox, water, firstaid, phone, oilcan, blt e demais itens |
| Radial ativo | PSRP_Inventory | RadMenu1..8, RadMiddle, PhoneIconRad, InventoryIconRad, EmoteIconRad etc. |
| Celular | PSRP_PHONE | PhoneTexture, PhoneWallpaper1..10, ContactIcon, MessagesIcon, NotificationsIcon, UberIcon, NotesIcon, ClockIcon etc. |
| F11 | PSRP_Banners | PSRPBANNER1, PSRPBANNER2, PSRPBANNER3 |
| Telas de trabalho da referência | PSRP_Jobs | jobsystemhome, jobsystemhighlight, workborder e imagens de missões |

Main inicializa RadialMenu.Instance e chama seu Update. A outra implementação UiSystem.DrawRadicalMenuBase não tem chamada encontrada no projeto exportado. O radial ativo usa PSRP_Inventory, não PSRP_RADICAL. PSRP_RADICAL, char_jmc e char_music existem no pacote, mas não apareceram como nomes literais nos fontes exportados; nomes também podem vir de variáveis/configuração, portanto isso não prova que nunca possam ser usados.

SettingsMenu.UpdateAnimation alterna PSRPBANNER1/2/3 a cada 5000 ms enquanto os menus de configuração estão visíveis. O contador totalFrames=4 volta a 1 antes de tentar 4. A altura do banner é 128 pixels virtuais, não Width/4. Isso foi alinhado no RPCore. AnimateSettingsBanner=False em Interface.ini mantém o carro preto PSRPBANNER2 fixo, caso desejado.

A varredura registra oito nomes literais não encontrados no OIV enviado, incluindo EngineIRad, PawnIconRad, JobsBanner, RadioBanner e GangIcon. Eles podem refletir estados iniciais, caminhos não usados ou diferenças de versão. Referências dinâmicas não são completamente resolvidas pelo script. Os vínculos do RPCore continuam usando nomes confirmados nos YTDs, sem copiar esses nomes ausentes automaticamente.

## Correção de integração

Na 0.5.0, Visuals.Has dependia de Catalog.json e os vínculos dependiam de Bindings.json externos. Se o usuário atualizasse somente a DLL ou faltassem esses arquivos, a UI podia falhar mesmo com os YTDs instalados. A 0.5.1 inclui na DLL os nomes/dimensões/vínculos padrão dos 679 recursos em oito dicionários. São metadados, não imagens: os pixels continuam vindo do GTA.

Os JSONs locais passam a ser overrides opcionais. Um catálogo parcial não remove imagens padrão; valores válidos personalizados prevalecem. Um JSON inválido é registrado no log e permanece intacto; os padrões internos continuam disponíveis. O arquivo nunca é sobrescrito para corrigir esse caso. Nenhum save ou YTD/RPF foi convertido ou modificado.

O aviso de textura não carregada agora só aparece depois de uma requisição explícita sem sucesso por dez segundos. Antes, bastava PSRP não estar carregado após o tempo inicial, o que podia acusar instalação quando o recurso nem tinha sido solicitado.

## Diagnóstico no GTA

1. Instale a atualização 0.5.1 com o GTA fechado, preservando os YTDs já instalados. Não precisa reinstalar o OIV para este ajuste.
2. Abra F10 > Visual assets: diagnose e espere aproximadamente dez segundos.
3. O RPCore solicita os oito dicionários, inclusive com a UI desligada, e consulta GET_TEXTURE_RESOLUTION para todas as texturas do catálogo. As consultas são divididas em lotes de 16 por frame.
4. O resultado fica em scripts/RPCore/VisualDiagnostics.json, com cópia anterior .bak, e resumo em RPCore.log. Inclui estado de dicionários, cada nome interno, tamanho esperado/retornado, flags de UI e registro dos modelos do DLC.

Interpretação:

- dictionary_not_loaded: o motor não informou carregamento após a solicitação.
- dimensions_match: dimensões retornadas coincidem com as do pacote de referência.
- different_resolution: o GTA retornou outra dimensão; pode ser substituição de recurso ou um valor de fallback. Não é rotulado automaticamente como imagem ausente.
- invalid_resolution / query_error: consulta não forneceu dimensão válida ou falhou.

As funções nativas não dizem de qual arquivo físico/RPF veio a textura. Dicionário carregado e dimensões iguais não provam pixels iguais nem posicionamento correto. A confirmação visual requer abrir I, F1, J, F11 e o HUD no GTA. Não houve execução do jogo neste ambiente.

## Validação e reprodução

Build net48 sem erros/avisos; 112 testes passam. Os 16 novos casos verificam metadados embutidos sem arquivos auxiliares, merge de overrides sem perder nomes, ciclo real de três banners, classificação das consultas e relatório JSON. Os dois recursos de metadados foram confirmados também no binário Release. Os 96 testes anteriores continuam passando.

A auditoria pode ser reproduzida com ILSpy e scripts/audit-psrp-textures.py --source <pasta-exportada> --dll <PSRP.dll>. O script é somente leitura da referência e não a executa. O fonte decompilado completo não foi incorporado ao RPCore; o repositório contém o mapa de referências e implementação própria.

As mudanças são de resolução/diagnóstico e banner. Funcionalidades ainda pendentes da 0.5.0 (telefonia, SMS, Uber, durabilidade, crafting e mapa de objetos) permanecem descritas em INTERFACE-0.5.0.md. O dlc.rpf de psrp_props continua sendo fonte dos modelos 3D, solicitado por Model no GTA, separado das texturas 2D do HUD.
