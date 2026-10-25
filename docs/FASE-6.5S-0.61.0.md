# RPCore 0.61.0 — fase 6.5S: venda automática de armas

## Instalação

Extraia RPCore-update-0.61.0.zip da branch rpcore-entrega. DLL pronta; feche GTA e abra PowerShell na pasta de Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Compatível com GTA Legacy, SHVDN Enhanced1.1.0.6 e LemonUI2.2. Preserva saves/configs existentes, .oiv/.rpf/.ytd/props, celular F2 e teleport F11. AutoArms.json só é acrescentado quando ausente. Não use PSRP.dll junto.

## Ativar por categoria

A venda automática começa **desligada**. Carregar/consultar um save antigo não cria configuração nem vende estoque.

1. Afiliado a uma gangue cadastrada e controlando uma área, vá a pé ao centro dela (Warehouse.Radius,12m padrão), em segurança, sem procurado/combate/missão/cutscene/ragdoll/atividade/contrato pendente.
2. **F4 → Comercio dos territorios → Arsenal / armas: área**.
3. Selecione **Auto Pistolas: DESLIGADA** para ativar. Há controles independentes para pistolas, SMGs, fuzis e escopetas disponíveis no catálogo. **Munição é excluída**, seguindo a PSRP.
4. O menu informa receita automática, preço atual de compra e valor da entrega presencial antes da seleção. Ativar confirma a preferência, sem cobrança e sem retirar arma naquele momento.
5. Feche os menus e jogue em segurança, livre de outros trabalhos/contratos. Pode sair da área e dirigir: a venda automática não exige permanência junto do arsenal após ativar.

Cada área tem **um relógio compartilhado**: a cada **180 segundos ativos**, vende **uma arma no total** dentre as categorias selecionadas com estoque, sorteada entre as disponíveis. Quatro categorias ligadas não geram quatro vendas por intervalo. Uma nova venda começa seu próprio intervalo completo.

## Valor e estoque

Receita padrão **fixa de $500 por arma**, como ProcessAutomaticWeaponDealing da PSRP. O valor é o mesmo para as quatro categorias. Exemplo de pistola padrão: compra$900, entrega presencial$1200, venda automática$500. Escolha a categoria e a forma de venda considerando esses valores; estoque recuperado por roubo também pode ser vendido automaticamente.

Na venda: **arsenal Stock−1 / Sold+1 / Revenue+500**, histórico automático Sales+1/Revenue+500/LastProduct e **receita suja da área Dirty/Proceeds+500**. Tudo é gravado junto na Wallet. Sem preço150% de drogas, banco/cash, salário, tesouraria, reputação ou relação direta.

O processamento85% existente continua manual: processar os$500 gera$425 no caixa da gangue, sujeito às condições anteriores. Não implementa lavagem automática nesta etapa.

Só usa arsenal comercial da gangue/área/perfil selecionados. Estoque pessoal de armas/munição, Dealer, armazém criminal de drogas e mercadorias legais permanecem em seus sistemas. Não cria comprador/NPC/prop nem inicia missão física. Compra e entrega presencial de armas, roubo de van e demais trabalhos continuam disponíveis; uma atividade/contrato em andamento impede consumo automático concorrente.

## Relógio e condições

Conta tempo da sessão em execução, sem timestamps persistidos:

- Jogador humano/vivo, seguro, sem procurado/combate/missão/cutscene; mesma filiação cadastrada e posse válida da área, sem disputa/iniciação pendente.
- ShiftGate livre: nenhum trabalho ou serviço ativo que use a trava compartilhada.
- Sem frete, lote de drogas/armas ou roubo pendente, mesmo suspenso. Conclua/cancele/estorne antes de voltar a contar.
- Apenas categorias selecionadas, conhecidas no catálogo e com estoque positivo. Se nenhuma tiver estoque, zera tempo sem receita.
- **Pausa, menus e modal congelam o tempo**. Gaps maiores que2s são ignorados. Não vende enquanto um menu está aberto, mesmo se já estava pronto.
- Perder condição de mundo/atividade/contrato/posse/filiação/estoque zera o intervalo em memória. A preferência continua salva; ao voltar a ficar apto, inicia um intervalo completo.
- Reload, troca de perfil/ator e alteração das categorias iniciam do zero. Cada área/perfil tem seu próprio controle; configurações de gangues antigas ficam conservadas, mas só sua filiação/posse atuais operam.

Sem acúmulo offline/catch-up ou várias vendas para compensar tempo ausente. Avançar o relógio do GTA pelo F11 não substitui segundos ativos da sessão. Em configuração padrão, espere três minutos com menus fechados e sem outra atividade. Para testes mais rápidos, pode editar AutoArms.json com GTA fechado para IntervalSeconds30 (mínimo permitido), recarregando depois.

## Desligar e acompanhar

No arsenal, selecione a categoria **LIGADA** para desligar. Isso zera o intervalo da área, sem apagar estoque/histórico.

No **comércio principal**, aparecem configurações ativas, produtos selecionados, vendas e receita acumuladas, com **Desligar auto / gangue / área**. Esse controle desliga todas as categorias daquele registro, inclusive depois de perder posse/filiação ou quando outra atividade impede ativar. Serve para encerrar preferências antigas sem acesso ao arsenal anterior.

Cada venda mostra notificação com área/produto/receita. Histórico da área aparece no arsenal. Dados inválidos, SKU removido, falta de estoque ou limites impedem crédito sem substituir arquivos. Falha de save restaura as três referências e suspende a área nesta sessão; confira log/dados, desligue/reative ou recarregue para tentar após intervalo novo. Não repete tentativas com débito por frame.

## Arquivos e dados

Novos **GangAutoArmsData.cs** (configuração, histórico, transações e relógio puro) e **GangAutoArms.cs** (menus, condições e agendamento em execução). Wallet/Economy têm livro opcional **GangAutoArms**. GangBusiness integra tick/controles/limpeza por perfil; GangBulk acrescenta controles no arsenal e preserva construtor anterior.

GangAutoArms.Accounts: Gang/Area/Products/Sales/Revenue/LastProduct, até640 registros e4 categorias distintas; Receipts limitados a256. Campos futuros preservados, IDs canônicos `pistol`, `smg`, `rifle`, `shotgun`; consulta não cria registro. Prefs e histórico persistem, relógios e recibo do ciclo corrente ficam só na sessão.

Venda confirma **AutoArms + Arms + Illicit em uma Wallet**, com rollback/replay, estoque revalidado, validação de dados/owner e overflow. Mantém **Stock=Purchased+Recovered−Sold** do arsenal. Configurar/desligar muda apenas preferências, sem efeitos financeiros. Configuração/livros inválidos são protegidos, sem reset automático.

Novo **Gangs/AutoArms.json**: IntervalSeconds30…1800 (180), UnitPrice1…100000 (500). Opções inválidas usam padrões em memória sem substituir arquivo. Preço/intervalo valem na sessão carregada e nas próximas vendas, sem reprecificar histórico. Demais configs/assinaturas mantidas.

## Referência PSRP e diferenças

Varredura PSRP.GangSpawner: TryGetAutomaticWeaponSale seleciona uma das quatro categorias disponíveis e excluiAmmo; ProcessAutomaticWeaponDealing usa180s, retira uma arma e adiciona$500 aTurf.Cash. Fonte usaDateTime.UtcNow e até40 vendas de catch-up. Auditoria PSRP-SYSTEMS-AUDIT-0.61.0.json registra75 fontes, hashes e evidências.

RPCore mantém valores/categorias padrão, mas exige **opt-in explícito**, seleção por categoria, tempo ativo e condições seguras/livres. Sem vendas offline/catch-up, NPCs de comprador ou lavagem automática. Receita vai ao livro sujo existente/processamento manual85%. Automação não garante lucro sobre compras pagas. Não se afirma paridade completa: lavagem automática/boost do café, convoys/refinamento/diálogo/guerras/polícia e fases8/9 continuam pendentes.

## Teste no GTA

- Antes de ativar, espere um período com arsenal abastecido: não deve consumir. Ative só pistolas, feche menus e aguarde180s ativos: local−1/Sold+1 e Dirty+500. Banco/cash, armas pessoais, munição, drogas e relações iguais.
- Deixe somente munição no arsenal: sem venda. Ative várias categorias com estoque: uma arma total por área/intervalo. Confira que nenhuma categoria desligada é consumida.
- Abra F4/F11/pausa antes de completar intervalo: sem contar esse tempo ou vender dentro do menu. Inicie trabalho, pendure frete/roubo/lote ou ganhe procurado: intervalo reinicia quando ficar apto. Vendas físicas/roubo anteriores devem funcionar sem consumo concorrente.
- Reload após completar uma venda não deve repetir; prefs/histórico permanecem e próximo ciclo começa do zero. Troque perfil/filiação e retorne: dados separados, sem catch-up offline. Desligue pelo comércio depois de perder posse e confira preservação de estoque.
- Processe$500 pelo fluxo anterior: $425 ao caixa. Confira histórico, saldos/estoques antigos, F2/F11 e compatibilidade com mods.

Build/dados/instalador não executam GTA. Tempo real de sessão, segurança/trava/menu/perfil e convivência com trabalhos/atividades precisam da sua validação no jogo.
