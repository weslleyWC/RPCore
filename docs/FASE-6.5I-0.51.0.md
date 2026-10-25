# RPCore 0.51.0 — fase 6.5I: comércio dos territórios

## Instalar

Baixe RPCore-update-0.51.0.zip da branch rpcore-entrega e extraia. Com GTA fechado, abra PowerShell na pasta que contém Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

DLL pronta no pacote, sem precisar compilar. Mantém GTA V Legacy, SHVDN Enhanced1.1.0.6 e LemonUI2.2. Não reinstala .oiv/.rpf nem executa PSRP.dll junto. Business.json só é instalado se ausente. Salvos/configurações anteriores, posses/defesas/iniciações, tesouraria/patentes, trabalhos, F2 e F11 preservados.

## Como trabalhar

1. Esteja afiliado a uma gangue cadastrada e controle um território. Pode usar uma área já pertencente à sua gangue; gangue própria precisa conquistar. Resolva/cancele disputas ou iniciações pendentes.
2. **F4 → Comercio dos territorios** lista apenas áreas atualmente disponíveis. Escolha **GPS** de uma delas; F11 → teleport ao waypoint continua disponível para agilizar seu deslocamento.
3. A pé e a até **12m do ponto**, sem procurado/combate/missão/cutscene ou outra atividade, escolha **Repor 10 unidades / $200**. Seu banco perde200, o estoque local ganha10. Limite padrão100.
4. Escolha **Atender 5 unidades / $175**. O menu fecha; permaneça ali por **45 segundos ativos**. A legenda informa progresso; pausa ou menus não contam. Ao ficar pronto, pressione **E** sem menu aberto.
5. O estoque cai de10 para5 e **$175 entram no caixa da gangue**. Banco e dinheiro em mão não recebem automaticamente. Consulte **F4 → Tesouraria / pagamentos**; pode retirar na HQ ou usar a participação por patente após o prazo existente.
6. Outro atendimento vende as5 unidades restantes. Depois precisa repor. Um lote comprado por200 rende350 após dois atendimentos: margem150, antes de qualquer refinamento de custos.

A reposição é uma compra direta no território nesta etapa. O atendimento representa gestão ativa; ainda não tem clientes NPCs, balcão/props ou entrega física de mercadorias. Não é emprego de café/logística completo nem reprodução dos negócios ilegais da PSRP.

## Interrupções, posse e saves

**END** ou Encerrar atendimento atual no F4 cancela apenas o tempo não confirmado. Entrar em carro, morrer, sair da área, combate/procurado, missão/cutscene, troca de personagem/perfil/afiliação ou perda da posse encerram o atendimento. Estoque só é consumido com E e gravação bem-sucedida. Reload/scripts/jogo fechado não preservam tempo de atendimento, não vendem estoque nem rendem dinheiro offline. Não há taxa de cancelamento.

Compras e receitas são separadas por perfil, gangue e território. Nomes de gangue ignoram caixa de letras. Perder a área bloqueia seu estoque ali; o novo dono não herda a mercadoria. Se a mesma gangue recuperar a área, acessa o estoque anterior. Sair da gangue não apaga contas antigas; não há confisco ou migração automática.

**Wallet.json → GangBusiness** guarda contas Gang/Area/Stock/Purchased/Sold/Revenue e recibos. Stock=Purchased−Sold. **GangTreasury.Accounts → BusinessIncome** registra receita do comércio, separada dos depósitos bancários. Contas antigas sem o campo continuam com zero: Balance=Deposited+BusinessIncome−Withdrawn−Salary. Venda atualiza os dois livros em uma gravação da carteira; falha restaura livros/banco originais. Dados desconhecidos são mantidos e dados inválidos bloqueiam as operações, sem sobrescrever saldos para corrigir.

O primeiro crédito em uma tesouraria nova inicia o prazo de24h do jogo. Uma conta já existente mantém sua referência; nova receita não reinicia o prazo. Retirada e participação continuam presenciais na HQ e usam o saldo disponível. Não há coleta automática nem crédito direto no protagonista.

**Gangs/Business.json** configura UnitCost20, UnitPrice35, PurchaseUnits10, SaleUnits5, Capacity100, ServiceSeconds45 e Radius12. Preços/lotes têm limites; ServiceSeconds10–600 e Radius5–30. Opções inválidas usam padrões em memória, sem substituir seu arquivo. Alterar o preço afeta vendas futuras; não reescreve contadores históricos. Estoque já comprado acima de uma capacidade reduzida não é apagado, apenas impede reposição até liberar espaço.

## Comparação com PSRP

GangSpawner.TurfSaveData separa drogas/armas/Cash/CleanCash. CollectTurfCash exige posse; NutCafeSupplyRun usa reposição com3 caixas; lavagem automática tem eficiência85%. Auditoria PSRP-SYSTEMS-AUDIT-0.51.0.json registra as evidências e diferenças.

Aqui há comércio legal genérico e ativo com receita ligada à tesouraria. Reposição com veículo/caixas, compradores, catálogo criminal/dinheiro sujo/lavagem, negócios específicos, guerra automática/diálogo/polícia seguem pendentes. Não importa Cash/CleanCash ou estoque PSRP. Fases8 (interface/mecânicas) e9 (refinar todos os trabalhos) permanecem reservadas.

## Teste no GTA

- Anote banco/caixa/estoque. Reponha10: banco−200/estoque+10; tente sem dinheiro, com estoque cheio, longe da área ou em território rival: nenhuma alteração parcial.
- Atenda45s e confirme E: estoque−5/caixa+175, banco e cash iguais. E repetido/reabrir/reload não repetem a receita. Faça segundo atendimento; terceiro sem reposição deve ser recusado.
- Antes de confirmar, teste END, menu/pausa, sair da área, carro, procurado e reload. Sem consumo/receita; retomada inicia tempo zero. Menus/pausa não devem avançar o serviço.
- Retire parte do caixa na HQ e teste participação existente. Troque de perfil/gangue: estoque/contas independentes. Uma perda de defesa bloqueia comércio até a mesma gangue retomar a posse.
- Confira F4 territórios/defesas/iniciações/equipe, emprego de pizza e tesouraria/patentes, banco/PDM/seguro/personagem, celularF2 e F11.

Build sem erros/avisos e1833 testes aprovados. Pontos no mapa/segurança de cada centro, tempo/teclas e compatibilidade dentro do jogo precisam da sua validação; GTA não roda nesta nuvem.
