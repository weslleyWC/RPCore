# RPCore 0.92.0 — inventário de armas e munição

Esta entrega continua o refinamento 8, baseada na PSRP.dll fornecida. Não conclui todos os menus nem antecipa a revisão de realismo dos trabalhos da fase 9.

## Referência verificada

`NewInventorySystem.InventoryItem.Weapon`, `UseSelectedItem`, `SyncWeapons`, `SyncAmmo`, `SaveInventory`/`LoadInventory`; `SettingsMenu` e as opções `Require Weapon In Inventory` (true) e `Hide Vanilla Weapon Wheel` (false); `ItemDatabase` e seus itens/ícones. A arma permanece no slot ao equipar. A referência dá até 250 balas em um caminho de uso e desconta munição no aperto de Attack, mesmo sem prova de um tiro; esses comportamentos não foram reproduzidos. Na referência SyncWeapons/SyncAmmo são chamados no estado Open do inventário.

## Uso

1. Instale usando o Install-RPCore.ps1 incluído no ZIP, a partir da pasta extraída. Não instale PSRP.dll junto do RPCore. Dependências continuam SHVDN Enhanced 1.1.0.6 e LemonUI 2.2 SHVDN3.
2. Entre com seu personagem e aguarde a pé, fora de missão/trabalho e com controle do personagem. Suas armas nativas atuais e munições serão registradas no Inventory.json do perfil, sem cobrança, descarte ou substituição de comidas/containers antigos. Não precisa ativar o sistema.
3. Abra o inventário com sua tecla configurada. ENTER sobre uma arma equipa e fecha o inventário. Ao usar novamente a arma que está na mão, seleciona desarmado; o item continua com você. Usar um item de munição apenas mostra a explicação: a reserva é automática.
4. Munição fica em item separado por tipo nativo realmente compartilhado. Pistolas e SMGs não são misturadas artificialmente; tipos especiais podem ter itens distintos com nomes de categoria semelhantes. Uma arma sem munição não ganha balas ao equipar. Recarregar redistribui balas entre reserva e pente sem consumir outra unidade do item.
5. Granadas, coquetéis e outros arremessáveis ficam em pilhas do próprio item; cada lançamento real reduz a pilha, inclusive a última unidade. Não há item extra fictício de balas para granadas.
6. A roda fica oculta por padrão a pé em livre exploração. F11 → Game Settings → Ocultar roda de armas do GTA permite voltar ao controle nativo. Inventário/munição continuam ativos com a roda visível. Em veículos, embarque, missões e atividades o RPCore cede os controles nativos, preservando o fluxo dos trabalhos.
7. Use os depósitos/porta-malas/apartamento já existentes para guardar ou recuperar arma e munição separadamente. Quantidades são salvas juntas na transferência; a arma é retirada do arsenal nativo quando deixa o inventário. Recuperar do depósito requer equipar o item. Cor, acessórios e limite do pente são registrados; munição do pente não é criada de novo. DELETE continua sendo descarte definitivo, não uma coleta física no chão.

## Preservação e limites

Armas usam IDs reservados weapon_ + hash e reservas ammo_ + tipo nativo. Inventory.Armory é opcional em saves antigos e conserva campos desconhecidos. A importação registra armas observadas uma vez, não recria armas descartadas a cada varredura e não soma a mesma munição por cada arma. Migração ignora o limite de peso para não perder equipamento existente: se ficar acima do limite, guarde munição em um depósito. Armas pesam zero como na referência; reservas .05 kg por unidade.

Aquisições nativas de armas ainda não registradas e mudanças de munição das armas acompanhadas são observadas. Uma arma já gerenciada que foi descartada/guardada não é reimportada automaticamente como nova cópia ao reaparecer no arsenal; recupere o item do depósito. Não há nova loja paralela de armas nesta entrega. Ferramentas como galão, extintor, scanner e paraquedas conservam o sistema nativo, acessível com a roda liberada quando necessário.

Em atividades/missões, a sincronização fica suspensa. Armas desconhecidas observadas nesse contexto são tratadas como empréstimo, sem virar patrimônio; só deixam essa exclusão quando não estão mais no arsenal. A primeira migração deve ser feita em livre exploração: não é possível distinguir retroativamente um empréstimo já presente no arsenal antes do primeiro cadastro de uma arma pessoal antiga. Munição persistida do inventário volta a valer ao retomar livre exploração; não se usa equipamento de missão para criar estoque. Troca de perfil/ator, morte, pausa e cutscene suspendem a rotina. Em veículos a munição pessoal continua sendo acompanhada, para não gerar tiros gratuitos ao sair; a roda nativa permanece livre e equipar/transferir/descartar pelo inventário exige estar a pé. Não há remoção global de armas, alteração de dinheiro, RPF/OIV/YTD ou teclas F2/J/F11.

Importação/munição usam cópia e gravação antes da confirmação em memória. Falha ao gravar a variação de munição conserva o baseline anterior para repetir o mesmo cálculo e bloqueia Attack somente naquele frame; erro no log, sem crédito duplicado. Arquivo malformado não é reparado silenciosamente. Ações do motor GTA e JSON não são uma transação única: queda do processo antes do próximo frame pode perder a última observação; não há garantia de atomicidade entre o save nativo do jogo e o mod. Acessórios/mods indisponíveis exigem manter os recursos instalados; nenhuma DLL de referência é executada.

## Roteiro dentro do GTA

- Antes de instalar, anote armas, munições, acessórios e saldos. Confira a migração sem balas extras e com comidas/slots/containers anteriores.
- Equipe/guarde pistola, SMG, rifle, escopeta, sniper, arma corpo a corpo e lançador. Duas armas de um mesmo tipo devem compartilhar a reserva sem duplicação. Teste arma vazia e mudança de perfil/protagonista/MP.
- Dê tiros isolados e rajadas, segure Attack sem poder atirar, mire e recarregue. Só o consumo real deve reduzir a reserva; pegar/comprar munição deve aumentar pelo delta real.
- Lance granadas até a última e teste nova coleta. Guarde parte de uma pilha no porta-malas e compare quantidades.
- Deposite arma com acessórios/cor e sua munição; confirme ausência na roda. Retire, equipe e confira acessórios e balas, sem ressuscitar pente antigo. Teste o armazenamento privado do apartamento.
- Alterne a opção da roda no F11 e reinicie o script: preferência deve permanecer. Teste teclado/controle, J, celular F2 e teleporte F11.
- Entre e saia de veículo, faça trabalho/caça/missão com equipamento temporário e volte à exploração. Nenhum equipamento deve ser removido durante a atividade e empréstimos não devem virar estoque.
- Salve/recarregue depois de tiros e transferências; verifique inventário, munição, dinheiro e saves de outros perfis. Faça também regressão da recuperação de sono da 0.91.

Validação automática: 3482 testes de dados aprovados (3449 anteriores +33 novos), build Release net48 sem erros/avisos. Testes novos cobrem migração, munição compartilhada/deltas reais, falhas de gravação, acessórios/metadados, armazenamento, pilhas de arremessáveis e proteção de atividades. Compilação e testes não homologam a física/IA/natives do GTA; roteiro acima precisa execução no jogo.
