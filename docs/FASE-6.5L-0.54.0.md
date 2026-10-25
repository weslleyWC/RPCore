# RPCore0.54.0 — fase6.5L: armazém criminal territorial

## Instalar

Baixe e extraia RPCore-update-0.54.0.zip da branch rpcore-entrega. Com GTA fechado, abra PowerShell na pasta que contém Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

DLL pronta, GTA V Legacy, SHVDN Enhanced1.1.0.6 e LemonUI2.2. Não reinstala .oiv/.rpf e não executa PSRP.dll junto. Warehouse.json só é acrescentado quando ausente. Configurações/saves anteriores, frete/estoque/receita/tesouraria, celularF2 e F11 mantidos.

## Preparar pacotes

Use o contato do tráfico já existente (menu de trabalhos → Trafico / vendas de rua → GPS do contato) e compre5 pacotes de um produto. A compra continua pagando banco e respeitando peso Dealer.json. Este armazém recebe **pacotes do estoque pessoal do tráfico**, não itens do inventário comum ou mercadorias legais do comércio/frete.

Filie-se a uma gangue cadastrada e escolha uma área sob seu controle. Resolva disputas/iniciações e conclua/estorne qualquer frete reservado. No F4 → Comercio dos territorios, use GPS da área e caminhe até o ponto.

## Guardar e retirar

**F4 → Comercio dos territorios → Armazem criminal: nome da área** abre o estoque local. Pode consultar e marcar GPS à distância; as operações exigem estar a pé a até12m do centro, sem procurado/combate/missão/cutscene/modal ou outra atividade. O menu precisa de posse e filiação válidas, sem disputa/iniciação pendente.

Cada produto mostra **pessoal/local**, peso, depositado, retirado e vendido. **Guardar1/5** retira essa quantidade do Dealer pessoal e acrescenta ao armazém da área. **Retirar1/5** faz o inverso. Não paga dinheiro, não compra produto nem concede reputação. Banco e cash permanecem iguais.

Capacidade padrão **30kg**, máximo **999 unidades por produto** e20 unidades por operação de API (botões expõem1/5). Retirar precisa caber na capacidade pessoal do Dealer.json, padrão10kg. Uma operação sem quantidade/espaço/limite não move parcialmente. Se reduzir a capacidade configurada abaixo do estoque existente, nada é apagado; venda/retire até caber antes de guardar mais.

O livro é separado por **gangue/área/perfil**. Outra área não compartilha o estoque. Perder a área ou sair da gangue bloqueia o acesso antigo, sem apagar produtos ou entregá-los ao rival. Se a mesma gangue recuperar a posse, volta a acessar a conta. Nomes de gangue ignoram diferença de letras maiúsculas/minúsculas; IDs de produtos seguem o catálogo Dealer com caixa de letras original.

## Vender diretamente do armazém

No mesmo menu, selecione **Vender do armazem** a pé/desarmado no ponto. Requer estoque **local** suficiente para o turno, padrão3 pacotes conhecidos do catálogo; o estoque pessoal não completa uma falta local.

O menu fecha e os compradores laranja do tráfico se aproximam. Permaneça numa calçada acessível dentro do raio padrão45m. **E negocia** e a entrega/recusa/denúncia/reputação continuam conforme Dealer.json. Cada venda confirmada retira só do estoque local, acrescenta o contador vendido e credita **dinheiro sujo da mesma área**. Pacotes pessoais, banco e cash não são consumidos/creditados por essa venda. O menu do comprador indica armazem.

Depois de encerrar o turno, use o processamento da0.53 no menu principal do comércio: até500 por60s ativos e E, líquido85% à tesouraria e taxa15%. Na HQ pode retirar/receber participação existente. Exemplo com venda inicial de70: sai1 do armazém e entram70 sujos; processar70 rende59 ao caixa/11 de taxa, com arredondamento cumulativo nas próximas operações.

Os modos anteriores continuam disponíveis: **vendas normais** no menu do tráfico usam pacotes pessoais e pagam banco; **Vendas ilegais: área**, no menu principal do comércio, usam pacotes pessoais e geram receita suja territorial. Somente **Vender do armazem**, dentro do armazém, usa estoque local. Encerrar/reload limpa a escolha do modo. Não existe conversão automática de pacotes ou vendas antigas.

Morte, veículo, abandono da área, combate/procurado, missão/cutscene, troca de perfil/ator/gangue ou perda de posse encerram o turno territorial. Estoque e vendas já confirmadas ficam salvos. Segurar END por1s encerra pelo comportamento do Dealer. Compradores e risco policial permanecem os existentes; nenhum NPC novo de trabalhador ou imunidade policial foi acrescentado nesta fase.

## Dados e configuração

Novo **Wallet.GangWarehouse**: contas Gang/Area e dicionários Stock/Deposited/Withdrawn/Sold por produto, mais recibos limitados. Stock=Deposited−Withdrawn−Sold. Contas antigas sem o livro continuam válidas; nada é criado/importado no startup.

Guardar/retirar grava Dealer+Warehouse na mesma Wallet; venda grava Warehouse+Dealer estatísticas/reputação+GangIllicit juntos. Save com falha restaura livros originais, sem débito parcial. Replay/reload não repete transferência/estoque/receita; recibos globais do Dealer impedem reaproveitar venda entre modos. Pacotes pessoais são preservados até na referência Stock durante venda local. Outros sistemas e metadados desconhecidos ficam intactos.

**Gangs/Warehouse.json**: Capacity1–1000kg (30), Radius5–30m (12), SaleRadius20–100m (45). Catálogo, preços, peso pessoal, duração do turno e chances continuam em Jobs/Dealer.json. Configuração inválida usa defaults em memória sem substituir arquivo. Produtos removidos do catálogo permanecem guardados, com peso conservador1kg/un; o menu avisa e só permite gerenciá-los quando o mesmo ID voltar ao catálogo. Não destrói ou converte SKU desconhecido.

## Referência e limites

PSRP.GangSpawner mantém TurfSaveData.Drugs; DepositDrugIntoTurf verifica posse/quantidade e remove itens antes de creditar estoque. CompleteStreetDrugSale retira do turf e gera Cash. A auditoria PSRP-SYSTEMS-AUDIT-0.54.0.json registra hash/evidências e diferenças.

O RPCore usa o catálogo/estoque do Dealer próprio, permite retirada e mantém os modos antigos. Não importa inventário PSRP, não adiciona estoque de armas, reposição criminal por veículo, confisco rival, vendas automáticas ou simulação UTC/boost do café. Guerras/diálogo/polícia e fases8 (interface/mecânicas) e9 (todos os trabalhos) continuam pendentes.

## Teste no GTA

- Anote banco/cash/pacotes; guarde5: pessoal−5/local+5, sem mudança financeira. Retire1: pessoal+1/local−1. Teste falta de pacotes e peso local/pessoal cheio: nenhuma transferência parcial.
- Deixe ao menos3 pacotes locais e inicie Vender do armazem. Venda1: local−1/vendido+1/receita suja+preço, pessoal intacto. Repita E/reabra/reload: sem duplicação. Se o armazém não tiver pacotes suficientes, estoque pessoal não deve completar o turno.
- Confira os modos bancário e territorial pessoal anteriores e o processamento85%/tesouraria após encerrar venda local.
- Troque área/gangue/perfil, perca posse e retome: contas isoladas e antigos pacotes conservados. SKU fora do catálogo deve permanecer guardado, sem venda/remoção automática.
- Confira frete/retomada/estorno, comércio legal, receita/caixa anteriores, trabalhos/defesas/iniciações, F2 e F11.

1934 testes aprovados e build sem erros/avisos. GTA não roda nesta nuvem; interface/área/teclas, compradores/caminhos/entrega/risco e convivência com mods precisam da sua validação.
