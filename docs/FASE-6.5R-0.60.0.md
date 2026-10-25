# RPCore 0.60.0 — fase 6.5R: roubo de van de armas

## Instalação

Extraia RPCore-update-0.60.0.zip da branch rpcore-entrega. DLL pronta; com GTA fechado, abra PowerShell na pasta de Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

GTA V Legacy, SHVDN Enhanced1.1.0.6 e LemonUI2.2. Preserva saves/configs anteriores, visuais .oiv/.rpf/.ytd/props, celular F2 e teleport F11. WeaponRaids.json só é acrescentado quando ausente. Não use PSRP.dll junto.

## Iniciar

Afiliado a uma gangue cadastrada e controlando uma área, vá a pé ao centro, a até Warehouse.Radius (12m padrão), seguro, sem procurado/combate/ragdoll/missão/cutscene/atividade. Conclua/cancele roubos/lotes e conclua/estorne fretes anteriores. Resolva disputas/iniciações pendentes.

**F4 → Comercio dos territorios → Roubar van de armas: área.** Precisa de outro grupo cadastrado com modelos válidos e catálogo/arsenal com capacidade para toda a carga. Prefere o dono padrão da área como rival quando diferente de sua gangue; senão outro grupo válido. Sem custo bancário, compra prévia ou estoque pessoal necessário.

Captura gangue/rival, área, local, três modelos de guardas, veículo/opções, capacidade por SKU e carga. Não concede estoque ao iniciar nem muda carga ao retomar. Usa um dos três candidatos do primeiro território PSRP, também usados no abastecimento criminal: La Mesa(887.2639,-991.1592,32.74689), centro(400.9214,-354.4539,46.81522), oeste(-1631.082,-124.8462,57.98261). A van procura rua próxima, a até60m do candidato, com chão/espaço válidos; não remove tráfego/objetos para caber.

## Encontro, roubo e retorno

1. Siga o GPS. Ao se aproximar de70m, prepara modelos de forma assíncrona; espera menus fecharem. Falha de modelo/rua/chão/espaço/tag suspende sem recompensa.
2. Aparece **uma van GBurrito2 própria estacionada e três guardas próprios ao redor**. Guardas atacam imediatamente, vida200/armor50/precisão35/pistola60 munições, sem dinheiro/drop de armas e sem tomar carros. Nenhum grupo global é alterado. A van não tem motorista nem rota móvel nesta base.
3. **Elimine os três**. Cada morte exige flag nativa de dano do jogador; não é perícia completa de autoria. Abate sem prova ou guarda/corpo desaparecido suspende para refazer o encontro. Antes dos três, entrar na van não confirma captura/carga.
4. Tome o volante **da van específica do encontro**. Modelo parecido/carro próprio não vale: usa identidade temporária da entidade. Com os três abates e você como motorista, captura salva `Package` e GPS marca a área. Não exige revista de corpo. Guardas próprios são limpos; nenhum item entra no inventário.
5. Dirija essa van de volta. Despiste a polícia nativa; **sem procurado/combate**, pare a menos de0,5m/s, a até**15m do centro da área**, como motorista. A prova de devolução `VanDelivered` é salva automaticamente com menus fechados. Não remove wanted/repara carro/vida/armas/relógio.
6. Saia e vá ao ponto do centro. **E a pé a até3m confirma o arsenal**. Só então armas/munição comerciais entram em Stock e histórico Recovered, o roubo termina e Completed aumenta. Banco/cash/drogas/receita suja/tesouraria/reputação/diplomacia e armas pessoais continuam nas regras anteriores, sem prêmio nesta etapa.
7. Use **Arsenal / armas** para consultar os produtos recuperados e vender pelo fluxo físico anterior. Venda gera receita suja/processamento85%, como na0.59. A compra paga continua disponível depois de encerrar o roubo.

## Carga

Sorteada uma vez, sem multiplicador por quantidade de territórios rivais:

- Sempre2–6 pistolas e100–250 munições comerciais.
- 75% de chance de1–4 SMGs;55% de1–3 fuzis;50% de1–3 escopetas.

Usa os IDs `pistol`, `ammo`, `smg`, `rifle`, `shotgun` do Arms.json. SKU sorteado ausente impede início/crédito até restaurar; não converte para outro. Capacidade por SKU é capturada de Arms.MaxStock, padrão999. Todos os produtos precisam caber; limites de chaves/contas/histórico e overflow também são revalidados antes de conceder. Alterar capacidade depois não altera a contratada. Catálogo precisa continuar válido; se impedir E final, devolução permanece salva.

Arsenal: **Stock=Purchased+Recovered−Sold**. Recovered ausente em saves antigos significa zero. Nada equipa/concede munição utilizável ao jogador; é estoque comercial. Aquisição gratuita não aumenta Spent/Purchased ou Revenue. Venda posterior pode consumir recuperados respeitando a mesma conservação.

## Suspender, retomar, falhar e cancelar

**END** suspende. No comércio principal aparecem status, GPS, retomar/suspender e cancelar roubo de armas. Roubos de drogas e de armas compartilham um slot; pendente mantém os bloqueios territoriais já existentes (compras/vendas/transfers/processamento/acordos/fretes/lotes/novos roubos). Não bloqueia globalmente todas as outras atividades quando suspenso.

- Antes da van devolvida: retome a pé e seguro na área. **Refaz os três guardas e a captura/viagem, mesmo se a van já tinha sido tomada**. Mesma carga/local/rival/modelos/capacidade, sem nova tentativa/rerrolagem. Não restaura veículo/handles antigos de save. Prazo reinicia na retomada.
- Depois da devolução salva: retome a pé e seguro na área; **E finaliza sem van ou guardas novos**. O desaparecimento posterior da van não invalida a devolução já confirmada.
- Reload/perfil/ator/perda de posse/missão/cutscene/não humano/falha técnica/identidade desaparecida suspendem conservando o progresso confirmado. Perda de posse impede concluir, mas permite cancelar.
- **Morte**, van destruída/inutilizada antes da devolução, abandono do encontro antes de capturar a van ou prazo ativo encerrado **falham**: descartam carga não creditada e incrementam Failed. Estoque anterior permanece. Abandono aplica quando já entrou no raio da van e depois se afasta mais deRadius (120m padrão) antes da captura. Depois de capturar não há limite de distância para fugir, mas o prazo continua.
- Prazo padrão1200s ativos até devolução; pausa/menu/gaps>2s não contam. Após devolução não expira. Abrir menu não congela o combate/mundo.
- **Cancelar** descarta só carga não creditada e conserva arsenal anterior. Sem taxa/estorno porque roubo era gratuito. Não muda relação nem recompensa reputação.

## Limpeza da missão

Van e guardas recebem token de propriedade temporário. Só entidades próprias com tag correspondente podem ser removidas. Não mexe no veículo que você usou para chegar. A van da missão fica reservada contra uso em outros trabalhos/garagem durante controle da missão/limpeza.

Suspender/finalizar limpa guardas próprios e coloca a van própria numa fila. **Nunca apaga enquanto há ocupantes ou se move≥0,5m/s**; quando vazia/parada e ainda com a mesma identidade, remove. Não força saída/teleporte. Após END dentro da van, saia e pare para permitir limpeza; retomada usa outra van. Tag perdida/handle reutilizado não é apagado. Novas ativações limitadas se houver8 vans próprias aguardando limpeza. Reload de perfil conserva limpeza; ao descarregar o script, recursos ocupados são liberados sem expulsar jogador/NPC. Essa convivência precisa de teste real.

## Arquivos e compatibilidade

Novos **GangWeaponRaid.cs** (mundo/menu/van/guardas/provas/limpeza) e **GangWeaponRaidData.cs** (opções/carga/transações/regras puras). GangBusiness integra tick/E/END/perfil/dispose. Wallet acrescenta wrappers. GangRaidAttempt agora tem Kind0=Drogas/1=WeaponVan, WeaponOptions e VanDelivered; Kind ausente continua droga. Runtime e handlers de drogas ignoram/rejeitam tipo de armas. Contadores gerais GangRaid incluem os dois tipos, com recibos limitados; assinaturas anteriores preservadas.

Devolução é checkpoint separado da captura. E final grava **Raid+Arms na mesma Wallet**, acrescentando Recovered/Stock e limpando contrato. Falha restaura as duas referências anteriores, replay não duplica. Campos desconhecidos e livros não envolvidos preservados; sem gravação financeira em GangState.

Novo **Gangs/WeaponRaids.json**: VehicleModel padrãogburrito2 (nome/modelo carro validado em execução), SecondsLimit120…1800 (1200), Radius40…150 (120),1…30 Sites distintos válidos (três candidatos). Arrays substituem padrões. Configuração inválida usa padrões em memória sem substituir arquivo. Contrato captura opções; mudanças só valem em novos contratos. Arms.json/Warehouse.json anteriores não são substituídos.

## Referência e diferenças

Varredura PSRP.GangSpawner: StartWeaponSupplyRun/GenerateWeaponSupplyLoot/SpawnWeaponSupplyRunEncounter/SpawnWeaponSupplyVanCrew/UpdateWeaponSupplyVanStolen/UpdateWeaponSupplyVanDelivery/CompleteWeaponSupplyRun. Fonte usa GBurrito2, armor50/accuracy35, faixas base/chances de carga acima e motorista retornando até15m. Não aplica WorsenGangRelationship nessa conclusão. Auditoria PSRP-SYSTEMS-AUDIT-0.60.0.json registra75 fontes, hashes e evidências.

Diferenças explícitas: PSRP tem motorista+3 passageiros, van em movimento/fuga e armas da gangue, aggro por mira/tiro/entrada, prop de caixa de madeira, multiplicador35% por território rival extra e MissionWin. RPCore usa **van parada, três guardas externos com pistolas/ataque imediato, todos os três abates com prova, sem prop preso à van/escala/recompensa de reputação**. Retorno exige parar/segurança/sem procurado e E final, com checkpoints/retomada/contabilidade. Usa primeiro conjunto de candidatos, não todas as listas por território. Não se afirma paridade total. Convoys móveis/refinamento/automação/boost/diálogo/guerras/polícia e fases8/9 continuam pendentes.

## Testes no GTA

- Anote banco/cash, armas pessoais e estoques. Inicie gratuito, confirme van+3 guardas, elimine os três, tome a van e retorne sem procurado/parado≤15m; saia/E≤3m. Antes do E final nenhum produto deve entrar; depois apenas arsenal/Recovered e Completed aumentam. Consulte estoque/venda/processamento anterior.
- E repetido/reload depois de concluir não cria outra carga. Carro próprio/mesmo modelo diferente não confirma captura ou retorno; não deve desaparecer seu carro de chegada.
- END/reload antes de devolver refaz guardas/captura/viagem com mesma carga. Depois de devolver, END/reload permite concluir a pé sem NPC/van novos. Van fora da área, em movimento ou com procurado não confirma retorno.
- Teste morte/destruição/abandono/prazo/cancelamento/perda de posse: sem estoque novo indevido e sem apagar arsenal anterior. Falha só nos casos definidos. Guardas sem prova/desaparecidos devem suspender, sem crédito.
- END dentro da van não deve apagá-la/ejetar ninguém; ao sair e ficar vazia/parada, deve ser removida. Teste passageiro e carro em movimento durante suspensão; só limpeza de entidades próprias. Confira provas e GPS/menus, convivência com tráfego/mods, F2/F11 e contratos antigos de drogas/armas.

Build/dados/instalador não executam GTA. Rua/chão/espaço, IA/autoria de dano/decorator, captura/condução/retorno e limpeza adiada exigem sua validação dentro do jogo.
