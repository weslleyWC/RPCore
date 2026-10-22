# RPCore 0.6.0 — GTA V Legacy

## Fase 1 — conveniências e mapa

[Atualização 0.6.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.6.0.zip) · [Código-fonte 0.6.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.6.0.zip) · [Comparação RPCore × PSRP e próximas fases](docs/FASES-0.6.0.md)

Pontos de compra corrigidos pelas coordenadas da PSRP.dll, incluindo LTD Davis, e catálogo ampliado para 20 conveniências. Configuração antiga padrão é corrigida em memória, sem apagar Shops.json ou saves. Posições personalizadas são preservadas. Ícones com nomes/cor/escala por categoria, depósito agora no mapa, pizzaria com ícone e GPS e entrega ativa com pacote amarelo. F6 continua iniciando/encerrando o turno. OIV e DLC já instalados continuam válidos.

Build sem erros/avisos e 124 testes aprovados. A 0.5.1 foi confirmada funcionando no GTA pelo usuário; a 0.6.0 ainda precisa do teste dos novos pontos no jogo. [Teste desta fase e sequência dos jobs](docs/FASES-0.6.0.md#teste-solicitado-agora). Post OP, Uber e demais empregos continuam nas próximas fases; o comparativo distingue a lógica existente dos menus e classes vazias.

## YTD instalados — correção 0.5.1

[Atualização 0.5.1](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.5.1.zip) · [Código-fonte 0.5.1](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.5.1.zip) · [Auditoria da PSRP.dll e mapa dos recursos](docs/AUDITORIA-YTD-0.5.1.md)

O destino confirmado pelo OIV/print é mods/update/update.rpf/x64/data/cdimages/scaleform_generic.rpf. O RPCore usa os dicionários/imagens já montados pelo GTA; não abre esse RPF pelo caminho do Windows. Nomes e vínculos agora vêm também embutidos na DLL: faltar um JSON auxiliar não bloqueia mais as imagens padrão. Overrides locais continuam preservados. O banner F11 alterna as três imagens originais a cada cinco segundos, com altura 128 da referência.

F10 > Visual assets: diagnose agora verifica o carregamento e a resolução de cada textura, grava scripts/RPCore/VisualDiagnostics.json e um resumo no log. O relatório precisa ser gerado dentro do GTA. Dimensões iguais não provam renderização idêntica. Não precisa reinstalar os YTDs já instalados para esta atualização. Build sem erros/avisos e 112 testes aprovados.

## Interface 0.5.0 — referências do Downloads.zip

[DLL pronta 0.5.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.5.0.zip) · [Código-fonte 0.5.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.5.0.zip)

**Velocímetro no centro inferior**, seis indicadores no canto superior direito com dinheiro/banco alinhados, **inventário I em grade 5×5**, **celular F1 com wallpaper do carro vermelho/relógio/ícones do print**, radial J com tamanho corrigido e F11 com posição/fonte do menu de referência. F2 mantém garagens e F10 mantém banco/perfis/diagnóstico. O pacote visual 0.3.0 continua válido.

[Instalação, funções disponíveis, limites e testes da 0.5.0](docs/INTERFACE-0.5.0.md). A composição foi corrigida no código e validada nos dados; a renderização final precisa do teste no GTA. Notas/calculadora/pesquisa funcionam; Uber, chamadas de voz e SMS continuam pendentes. O inventário mostra durabilidade n/a; editor de objetos e crafting continuam pendentes. Não foi transplantada toda a lógica da PSRP.dll.

Configurações anteriores são preservadas. Os valores padrão antigos de NeedsTop (60/12) são reconhecidos em memória como o novo topo 20. As opções novas de Interface.ini usam defaults compatíveis mesmo se o INI antigo já existir. A unidade do desenho é mph por padrão, alterável em F11 > Vehicle Settings. A lista de inventário anterior permanece disponível no F11 > UI Settings.

## Histórico — interface 0.4.0

[Baixar atualização 0.4.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.4.0.zip) · [Código-fonte 0.4.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.4.0.zip)

**J** abre o radial com oito setores originais do OIV, Q/E seleciona e Enter abre. **F11** abre Settings Menu com as oito categorias dos prints e o banner do carro preto. **F2** abre garagens; F10 continua com perfis/banco/diagnóstico. O HUD passa a seis indicadores reais e dinheiro compacto; inventário pessoal tem cabeçalho azul.

O usuário confirmou que a 0.3.0 funciona; esta nova composição ainda precisa do teste visual no jogo. Veja [ações implementadas, limites e testes da 0.4.0](docs/INTERFACE-0.4.0.md). Map Builder nesta versão edita pontos pessoais com GPS; não coloca objetos. A PSRP.dll foi usada como referência de interface e não deve ser instalada junto do RPCore. Não foram copiadas todas as suas funcionalidades.

O pacote visual 0.3.0 continua válido e não precisa ser reinstalado se já estiver funcionando. O instalador da atualização preserva saves/configurações e acrescenta Interface.ini quando ausente.


Projeto C# .NET Framework 4.8 para **ScriptHookVDotNet Enhanced 1.1.0.6** e **LemonUI SHVDN3 2.2**, as versões informadas pelo desenvolvedor. Implementação própria sobre os fontes enviados, mantendo `ISystem`, LemonUI e os formatos básicos de inventário, necessidades e pizza.

A primeira entrega (commit 6819140) foi testada no GTA pelo usuário e ele informou que funcionou. As adições 0.2.0 e a integração visual 0.3.0 foram compiladas/testadas nos dados e aguardam seus testes de gameplay.

## Histórico — recursos visuais da 0.3.0

A DLL agora usa os recursos **PSRP.oiv** e **psrp_props.zip** enviados pelo usuário: HUD de anéis/ícones, painel de veículo, banners, ícones de itens, apresentação de props e telefone F1 com atalhos para os sistemas existentes. Instalar somente a DLL não registra texturas ou DLC no GTA.

Baixe também **RPCore-visual-assets-0.3.0.zip**. Se já tiver esses mesmos recursos instalados, use F10 → Visual assets: diagnose antes de reinstalar. Instruções completas e checklist: [VISUAIS-0.3.0.md](docs/VISUAIS-0.3.0.md). O helper prepara o DLC sem editar update.rpf; a instalação do OIV e a entrada dlclist são feitas pelo OpenIV, preservando os outros mods.

`Visuals.ini` controla a aparência, escala, props e tecla do telefone. Sem o OIV/DLC, o RPCore conserva a interface básica e as funções existentes. Os originais estão em visual-assets no repositório e no pacote visual separado; o ZIP de fontes não duplica esses binários. Os arquivos de arte não incluem toda a lógica/layout original PSRP; a composição foi implementada para o RPCore.

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
| F1 | Celular: setas/Enter para aplicativos, Backspace para voltar, F1 para fechar (a pé) |
| I | Inventário em grade: setas/Enter; C para reorganizar; I/Backspace fecha |
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
| X | Ligar/desligar motor como motorista |
| C | Ativar/cancelar cruise acima de 29 km/h; freio/acelerador também cancelam |
| B / N | Seta esquerda / direita |
| H | Pisca-alerta |
| K | Menu do veículo: motor, cruise, setas, capô e porta-malas |
| G | Iniciar/parar abastecimento no posto, parado e com motor desligado |
| F2 | Garagens: GPS, compra, guardar/retirar e serviços |
| J | Radial: Q/E, Enter; J/Esc fecha |
| F11 | Settings Menu: oito categorias de configuração |
| F12 | Concessionária: GPS e compra junto ao PDM |

Altere as teclas em `Settings.ini` se conflitam com trainers ou outros mods. Use F10 → Reload configuration para reler opções e teclas. Para alterar quais sistemas ficam habilitados, reinicie o script após editar `Features`. Os novos controles, combustível e garagem usam `Vehicles.ini`; as teclas de motor, cruise e setas de `Keys.ini` têm prioridade se estiverem presentes. `Car Controller Key = N` do PSRP continua sem efeito; K abre o menu de veículo, evitando a colisão com a seta direita N.

## Dados e compatibilidade

- `Items.json` e `Shops.json` mantêm os formatos originais. `Inventory/*.json` permite listas adicionais de itens; o último arquivo em ordem alfabética pode substituir um ID do catálogo base. IDs desconhecidos nos saves são preservados.
- `SaveData/<perfil>` mantém necessidades, inventário, pizza, conquistas, carteira e afiliação. `Inventory.json` também contém os compartimentos de armazenamento, para transferir itens e salvar ambos os lados em um arquivo atômico. Há `.bak` da versão anterior. Um JSON que falhou na leitura fica protegido contra gravação automática naquela sessão.
- Saves antigos do RPCore diretamente em `SaveData` são copiados uma única vez para o primeiro perfil detectado. `FlatMigration.json` registra o destino; os originais permanecem intactos. Faça a primeira inicialização com o protagonista dono desses saves.
- Michael, Franklin e Trevor têm perfis separados. Peds freemode usam o perfil selecionado no F10, persistido em `ActiveProfile.json`. Modelos desconhecidos/transições mantêm o último perfil. O RPCore não troca aparência ou modelo automaticamente.
- Arquivos originais do PSRP ficam em `assets/RPCore` nos fontes e em `scripts/RPCore/Legacy` no pacote, com áudio e gangues nos diretórios correspondentes. F10 importa os três jogadores enviados ou saves encontrados em `scripts/PSRP/SaveData/Players`. A importação nunca sobrescreve um perfil existente: cria `PSRP_<nome>` e retém uma cópia byte a byte do JSON completo, com todos os 151 campos.
- Importação funcional: fome, hidratação, dinheiro, banco, gangue/reputação, carteira de ações e conquistas já desbloqueadas. Campos de aparência, veículos PSRP, negócios, dívidas e territórios são preservados na cópia original; não são aplicados por esta versão. O novo cadastro de veículos RPCore é separado dos veículos PSRP antigos. A importação não entrega dinheiro de um save MP para um protagonista.
- `Wallet.json` é o checkpoint principal de dinheiro MP, banco e bolsa. Os protagonistas usam o dinheiro nativo do GTA; o salvamento desse dinheiro depende também do save do próprio jogo. A carteira em `StockMarket/<perfil>.portfolio.json` é exportada como cópia compatível; a bolsa RPCore é uma simulação local com três ativos e variação por minuto, sem integração com BAWSAQ/LCN.
- Porta-malas são identificados por modelo + placa dentro do perfil. Duas cópias do mesmo modelo com a mesma placa compartilham o compartimento; use placas distintas. O baú padrão está em `(-14.1, -1440.2, 31.1)`, editável em `Storage/Locations.json`. Limites: 100 kg no porta-malas e 200 kg no baú padrão.
- Gangues leem os três registros de `Gangs/CustomGangs.json`: nome, descrição, HQ e cor. Entre a pé no HQ pelo menu; cada entrega de pizza concede reputação até 100. Esta versão não cria NPCs armados, não muda relacionamentos globais nem simula guerras territoriais.
- Rádio usa Windows Media Player COM e conexão de rede no computador do GTA. Instale/habilite esse componente do Windows se necessário e forneça URLs diretas de áudio MP3 em `CustomRadioStream.xml`. O endereço fornecido no PSRP aparenta ser uma página de rádio, não um fluxo MP3 garantido; falhas de conexão são exibidas e registradas. Desligue o rádio nativo manualmente para evitar sobreposição. Reprodução real exige teste no Windows.
- Áudios de cinto, compra, entrega e conquista usam os WAVs PCM enviados, sem conversão dos originais. Os demais WAVs estão preservados. Os dois `RivalDealer*.wav` são IEEE float: não são enviados ao `SoundPlayer`, que espera PCM.
- A versão 0.2.0 acrescenta motor/cruise/setas, combustível e garagem próprios, com opções independentes em `Vehicles.ini`. Se outro mod já controla combustível, use `[Features] Fuel = False` e reinicie o script. O RPCore não reescreve handling, limite nativo de velocidade, polícia, armas, mapas ou configurações de outros mods. As opções PSRP de sistemas ainda ausentes permanecem sem efeito automático.

## Veículos, postos, garagem e concessionária (0.2.0)

- **Motor e setas:** ações somente como motorista de carro/moto/quadriciclo. O menu K abre capô e porta-malas com o carro parado. Setas e controle de auto-start alterados pelo RPCore são restaurados/liberados ao sair, trocar de veículo ou recarregar.
- **Cruise:** C registra a velocidade atual acima de 29 km/h e fornece aceleração para mantê-la aproximadamente. Frear, acelerar manualmente, puxar freio de mão, colidir, sair do chão, parar o motor ou abrir um menu cancela. Não é piloto de direção nem freio automático de descida e não modifica handling/max-speed.
- **Combustível:** 26 postos com blips, listados em `Fuel/Stations.json`. Pare na área, desligue o motor com X e pressione G; G novamente interrompe. Também é possível abastecer a pé a até 5 m do carro. O preço é por litro, arredondando a cobrança da operação para dólares inteiros; saldo e espaço no tanque limitam a quantidade. O consumo padrão é 8 L/100 km mais 0,8 L/h em marcha lenta. Tanque vazio desliga o motor, mantendo a possibilidade de reabastecer. Pausa, cutscene e missões do GTA suspendem o sistema. O tanque inicial usa o valor nativo do veículo, não recebe combustível gratuito ao recarregar. HUD mostra litros e distância rodada pelo RPCore.
- **Exclusões:** bicicletas, aeronaves, barcos, trens, elétricos e modelos de `SaveData/ExcludedGasList.json` (ou o original em `Legacy/SaveData`) não recebem consumo/intervenção RPCore. Não há recarga elétrica nesta versão. Combustível é identificado por modelo + placa e fica em `Wallet.json` → `Transport.Tanks` por perfil.
- **Garagens:** F11 mostra GPS para três localizações: Strawberry ($10.000/3 veículos), Burton ($25.000/6), Sandy Shores ($8.000/2), editáveis em `Garages/Locations.json`. Compre perto do marcador; vagas incluem veículos associados que estão fora. Guarde parado, como motorista, sem passageiros/reboque. O jogo recebe uma tarefa de saída; mantenha-se perto por até 15 segundos. O snapshot é salvo antes de remover a entidade; uma marca pendente permite recuperar uma interrupção sem criar automaticamente outra cópia.
- **Retirada/recuperação:** usa a saída configurada e recusa saída bloqueada. Primeiro procura modelo/placa no mundo; se o veículo existir, marca GPS em vez de duplicar. Se não existir, recria o último snapshot salvo. Modelo de addon ausente não apaga o cadastro. Não há seguro nem recuperação de estado posterior ao último save. Placas repetidas não são suportadas para veículos diferentes do mesmo modelo.
- **Personalização/danos:** preserva placa/estilo, cores normais e RGB, cores extras, roda, até 50 slots de mods disponíveis, variações, toggles, extras, neon, fumaça, película, livery, saúde de motor/carroceria/tanque, sujeira, combustível, pneus furados, vidros e portas quebrados. A forma exata de amassados, marcas de balas, ocupantes e flags/scripts de outros mods não é serializada. Guarde veículos parados sem ocupantes; veículos de missão são recusados por padrão. `AllowMissionVehicles = True` é uma opção explícita para veículos de seus mods/trainer; não muda a proibição durante missões ativas.
- **Serviços:** na própria garagem, como motorista parado, reparar custa $500 e lavar $25. Reparar preserva o combustível. Não há reparo automático gratuito nem intervenção em veículos de missão protegidos.
- **Concessionária:** F12 define GPS para o PDM; perto da entrada, permite comprar Faggio, Asea, Blista, Dominator e Sultan. `Dealership/Vehicles.json` aceita outros modelos/preços. É necessário ter vaga em uma garagem própria; a compra registra o carro, gera uma placa RP única e entrega na saída. Modelo ausente, saldo insuficiente ou saída bloqueada não devem cobrar. Isso implementa compra pessoal de veículos; a empresa PDM, funcionários e contabilidade empresarial do PSRP continuam pendentes.
- **Persistência:** garagem, veículos e combustível usam `Wallet.json` → `Transport`. Para peds MP, o pagamento e esses dados usam o mesmo checkpoint atômico da carteira. Nos protagonistas, o dinheiro nativo também depende do save GTA, como antes. A atualização lê saves 0.1 sem conversão destrutiva. As conquistas PSRP de primeiro carro, primeira garagem e primeiro abastecimento agora têm gatilhos.

O checklist específico da nova DLL está em [TESTES-0.2.0.md](docs/TESTES-0.2.0.md). Configurações antigas são mantidas; as novas ficam em arquivos separados copiados pelo instalador.

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

Se `.deps` existir, ela tem prioridade. Use `-p:ReferenceDir='D:\referencias'` para indicar outra pasta com as duas DLLs de referência. `python scripts/package.py` gera `dist/RPCore-update.zip` e `dist/RPCore-source.zip` após a compilação; com os originais em visual-assets também gera `dist/RPCore-visual-assets.zip`. Nenhum teste externo afirma ter executado o GTA.
