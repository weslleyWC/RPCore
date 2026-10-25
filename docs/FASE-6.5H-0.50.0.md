# RPCore 0.50.0 — fase 6.5H: progressão e tesouraria da gangue

## Instalação e acesso

Baixe e extraia RPCore-update-0.50.0.zip da branch rpcore-entrega. Abra PowerShell na pasta que contém Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Mantém GTA V Legacy, SHVDN Enhanced 1.1.0.6 e LemonUI 2.2. Não reinstala .oiv/.rpf nem executa PSRP.dll junto. Gangs/Treasury.json só é acrescentado quando ausente. Gangues/posses/defesas/iniciações/equipe, saves/configurações anteriores, DMV C/M1/A e F2/F11 preservados.

**F4** mostra a patente e a reputação. **F4 → Tesouraria / pagamentos** permite consulta e GPS da HQ. Depósitos, retiradas e participação exigem estar **a pé a até 5 m da HQ da sua gangue**, afiliado e cadastrado, sem procurado/combate/missão/cutscene/interface modal ou atividade reservada. Não precisa ligar a presença de NPCs para usar o caixa na HQ.

## Reputação e patentes

O limite antigo de 100 foi removido do ganho e do reload. Valores positivos já salvos acima de 100 são conservados. Não recria pontos que já tenham sido truncados por versões anteriores, nem concede retroativamente recompensas de capturas antigas.

| Pontos | Patente | Participação do caixa |
| --- | --- | --- |
| 0–249 | Recruta | 5% |
| 250–749 | Soldado | 10% |
| 750–1499 | Subchefe | 20% |
| 1500+ | Líder | 35% |

A patente alcançada não cai por perder reputação dentro da mesma filiação, como na referência. Isso inclui um save legado com reputação alta e sem o campo Rank. O título afeta a porcentagem; não troca o líder NPC, concede território ou amplia a equipe de três.

A partir desta versão, confirmar **captura dá +50**, confirmar **defesa vitoriosa dá +25** e perder uma **defesa ativa tira 20**, com piso zero. Reputação/patente ficam na mesma transação de resultado/posse: replay/reload não repete o ganho e falha de save não confirma parte do resultado. O ponto anterior por entrega de pizza afiliado continua, agora sem teto100. Falha de gravação desse bônus é registrada sem desfazer pagamento/entrega.

Join/Leave e conclusão das iniciações mantêm o comportamento anterior de iniciar reputação zero; agora também reiniciam a patente em Recruta. Não transfere a patente de uma gangue para outra. Sair da gangue não apaga suas contas de tesouraria ou muda territórios; valores da conta antiga ficam ligados àquela gangue/perfil e só são acessíveis afiliado a ela.

## Caixa financiado explicitamente

Esta etapa é um **caixa da gangue por perfil**, financiado com seu banco. Não é renda passiva ou dinheiro criado pelo mod. Não conecta ainda venda/estoque/lavagem dos territórios. A referência usa CleanCash dos territórios para salário; aqui a origem é explicitamente o depósito bancário até integrar esses negócios.

Na HQ, **Depositar $100/$1000/$10000 do banco** debita seu banco e credita o caixa da gangue escolhida na mesma gravação de Wallet.json. **Retirar** faz o caminho inverso, até o saldo disponível. Sem taxa, dinheiro em mão ou mudança no saldo nativo do protagonista. Não acessa automaticamente banco do PDM, cofre de moradia ou conta de outra gangue.

Os valores das linhas respeitam MaxTransfer (padrão $1.000.000 por operação de API; os botões expõem os três valores). Banco/caixa precisam caber em inteiros e os contadores usam limites/conservação. Saldo inválido ou arquivo corrompido é protegido. Dados desconhecidos e referências de outros sistemas da carteira são conservados.

## Participação por patente

O **primeiro depósito** inicia o prazo, padrão **24 horas do calendário do GTA**. Depois, na HQ, **Receber participacao da patente** transfere 5/10/20/35% do **saldo disponível naquele momento** para seu banco, debitando o mesmo valor do caixa. Arredonda para baixo; saldo insuficiente para render pelo menos $1 não gera pagamento.

Exemplo: deposite $1000. Após o prazo, um Recruta recebe $50 no banco e o caixa fica $950; banco + caixa permanece igual. Retirar depois os $950 também não cria dinheiro. Um Soldado receberia $100 do mesmo saldo inicial.

Cada coleta inicia novo intervalo a partir da hora atual. Ficar muitos dias ausente não acumula várias parcelas: paga uma porcentagem uma vez. Reabrir menu/reload/repetir recibo não paga novamente. Voltar o relógio não antecipa o próximo pagamento; se o save do GTA voltou a uma data anterior à última coleta registrada, precisa alcançar essa referência mais o prazo. O script apenas lê o relógio nativo, não altera data/hora do mundo.

Não há transferência automática em Tick, salário com caixa vazio ou cobrança automática ao carregar. O menu mostra saldo, patente/percentual e a última referência de prazo. Contas são isoladas entre perfis e nomes de gangue com caixa de letras diferente não duplicam a conta.

## Saves e validação

**GangState.json** ganha Rank opcional e mantém Reputation. Promoções de resultados de território são gravadas junto ao resultado/recibo. Patentes de saves antigos são inferidas para exibição sem gravar promoção automática ao iniciar.

**Wallet.json → GangTreasury** guarda contas por gangue, Balance, LastPaydayTicks, Deposited/Withdrawn/Salary e recibos limitados. O saldo deve corresponder a depositado − retirado − pago. Operações clonam só o livro da tesouraria e alteram o banco, salvam antes de confirmar e restauram banco/livro originais se falhar. Não dependem de gravar dois arquivos para transferir dinheiro; patente é lida do estado atual, sem alterá-la na coleta.

Novo **Gangs/Treasury.json**: PaydayHours (1–168, padrão24) e MaxTransfer (100–10.000.000). Para agilizar o teste, pode colocar PaydayHours=1 com jogo fechado e reiniciar scripts. Ainda precisa passar uma hora do jogo, normalmente cerca de dois minutos reais. Não adiciona botão que modifique o relógio/globalmente afete outros mods. Configuração inválida usa defaults em memória sem substituir arquivo.

## Comparação e limites

PSRP GangCreators.AddPlayerGangReputation usa 250/750/1500 e mantém maior patente; AddGangRecord usa Capture+50/DefenseWin+25/Loss−20. GangSpawner.GetGangSalaryPercentage/UpdateGangDailySalary usam 5/10/20/35% e 24h. Evidências/hashes em PSRP-SYSTEMS-AUDIT-0.50.0.json, com índice das 75 fontes.

Diferenças próprias: depósitos/retiradas bancários explícitos, participação solicitada presencialmente, caixa separado financiado pelo jogador e ledger conservativo. Não reproduz salário automático de CleanCash, renda/estoque de território, comércio/lavagem, conta global entre personagens ou migração financeira PSRP. Guerra automática, diálogo completo e polícia continuam pendentes. Interface/mecânicas continuam na fase 8; refinamento de todos os trabalhos na fase 9.

## Testes no GTA

- Confirme reputação atual e patente no F4. Ganhe captura+50/defesa+25; ao perder defesa, confira −20 com piso0. Passe 250/750/1500 e recarregue: sem truncar para100 ou perder patente ao cair abaixo do limiar.
- Na HQ, anote o banco, deposite1000 e confira banco−1000/caixa+1000. Retire100 e confira o inverso. Tente longe da HQ, com atividade, saldo insuficiente ou sem filiação: sem débito parcial.
- Para teste curto, PaydayHours=1. Tente receber antes da hora e depois dela: porcentagem da patente sai do caixa e entra no banco uma vez. Confira banco+caixa constante, sem pagamento adicional ao reabrir/reload.
- Confira que datas voltando não liberam outra coleta e que prazo longo paga uma parcela, sem multiplicar períodos offline. Sem saldo não gera dinheiro.
- Troque de gangue/perfil: contas/prazos independentes, contas antigas conservadas e patente/reputação de nova filiação reiniciadas conforme Join anterior.
- Confira capturas/defesas/posses, iniciações/equipe, empregos/pagamento de pizza, carteira/banco/PDM/seguro/personagem, F2 e F11.

Build sem erros/avisos e 1803 testes aprovados. Calendário/HQ/banco/menu e coexistência com mods precisam do GTA real; a nuvem não executa o jogo.
