# RPCore Bank no celular — 0.101.0

Implementação solicitada pelo usuário fora do grupo de refinamento: banco próprio no celular, com identidade RPCore e visual roxo/branco inspirado no estilo Nubank. São telas reais desenhadas pelo mod, não um atalho que fecha o telefone para abrir F10. Não utiliza serviço bancário externo, rede ou nova conta de dinheiro.

## Acessar e usar

Abra o celular pela tecla configurada (**F2 por padrão**) e selecione o cartão roxo **RPCore Bank**, acima dos12 ícones anteriores. O banco recebe foco inicial ao recarregar; com setas/controle, cima na primeira linha chega ao banco e baixo no banco volta à primeira linha de apps. Os12 ícones anteriores permanecem nas mesmas posições e continuam acessíveis. Contato de banco, busca e atalho bancário das configurações do próprio celular também abrem o app; F10 > Banco continua disponível separadamente.

A tela mostra o **saldo em conta** e o dinheiro **na carteira**, sem abreviar em K/M. Há quatro opções:

- **Depositar:** digite o valor que deseja mover da carteira para a conta.
- **Sacar:** digite o valor que deseja mover da conta para a carteira.
- **Depositar tudo:** prepara o total disponível da carteira que cabe na conta.
- **Sacar tudo:** prepara o total disponível da conta que cabe na carteira.

Valores inteiros podem ser digitados como `1000000` ou `1.000.000`. Não há valores fixos de menu, taxa, limite diário, cooldown bancário ou teto artificial por operação. Preserva o formato atual de saldo do RPCore/GTA: números inteiros até **2.147.483.647 em cada saldo**. Isso não foi trocado por saldo ilimitado em outro formato, para manter compatibilidade com GTA, saves, trabalhos, compras e demais sistemas. Valores acima do saldo de origem ou que excedam a capacidade do destino são recusados, sem cortar silenciosamente o valor digitado.

Antes de mover dinheiro, o app mostra o valor e os saldos resultantes. **Cancelar / voltar** começa selecionado. Selecione a segunda linha para confirmar. Após a gravação, aparece o comprovante da operação e os saldos atuais; o comprovante é apenas desta sessão do celular, não um extrato persistente. Backspace/clique direito/B cancela a conferência e volta à conta. A tecla do celular fecha e descarta qualquer operação ainda não confirmada.

## Dinheiro e persistência

Usa os mesmos `Wallet.DepositSaved/WithdrawSaved` e `BankTransfer.Commit` já usados no banco F10: a carteira do protagonista continua sendo o dinheiro nativo, e perfis RPCore continuam usando sua carteira própria. O saldo em conta é o mesmo do Wallet.json. Não cria conta paralela, bônus, cobrança, renda, empréstimo ou arquivo de saldo novo. Valores e campos existentes permanecem com o mesmo significado.

A conferência é ligada ao perfil, ator, modelo e aos dois saldos que foram apresentados. Se qualquer um mudar antes da confirmação, o pedido é descartado e o jogador precisa revisar uma nova operação. A cotação só pode ser consumida uma vez, inclusive se o botão repetir, a transferência for recusada ou a gravação lançar exceção. O pipeline anterior faz rollback comum dos dois saldos em erro de gravação; não há repetição automática. Como já ocorria no banco F10, dinheiro nativo e arquivo não constituem uma transação atômica contra queda abrupta do processo.

As conquistas bancárias existentes continuam sendo atualizadas após o pagamento confirmado. Falha ao salvar conquista é isolada da transferência: não transforma depósito já salvo em um pedido para cobrar novamente. O novo app não precisa escrever Phone.json para movimentar saldo. O celular mantém os guardas anteriores de segurança: fecha em missão, perda de controle, combate, morte, troca de perfil ou veículo em movimento; motorista parado permanece permitido conforme o telefone existente.

## Apresentação e compatibilidade

Tema roxo com identidade **RPCore Bank/rp**, painel claro, botões com foco e telas de confirmação/comprovante. Usa primitivas do GTA e o frame/papel de parede anterior; não exige trocar OIV/YTD/RPF nem instalar novas texturas. O relógio/dia da home ficam compactos para acomodar o novo acesso acima dos apps existentes. Mouse, teclado e controle usam as mesmas coordenadas/escala do desenho. O clique exige pressionar e soltar no mesmo alvo; entrada bancária e transições exigem liberar o botão antes de outra ação.

Consultados CustomPhone e depósito/saque digitado de MoneyTerminal da PSRP.dll para entender os fluxos anteriores. A identidade visual roxa e as telas bancárias são implementação própria pedida pelo usuário; não incluem empréstimos, juros semanais, Pix real ou outras rotinas da referência. Fases8/9 e os refinamentos pendentes seguem abertos. A correção de portas0.100.1 está incluída.

## Testar dentro do GTA

1. Instale com GTA fechado pelo instalador da pasta extraída. Abra F2 e confira o acesso roxo, os12 apps anteriores e os saldos que você já tinha.
2. Deposite um valor digitado, confira saldo projetado, cancele e verifique que nada mudou. Repita e confirme: carteira diminui, banco aumenta no mesmo valor. Saque e confira o movimento inverso.
3. Teste **depositar tudo/sacar tudo**, inclusive carteira ou conta zeradas. Teste valor alto com saldo suficiente, valor maior que o saldo, zero, negativo, decimal e cancelamento da digitação. Nenhuma taxa ou repetição automática.
4. Segure Enter, mouse ou botão de confirmação; faça cliques rápidos e pressione/solte fora do botão. Não deve transferir duas vezes. Confirme apenas depois de escolher a segunda linha.
5. Abra a conferência e altere o saldo por outro sistema, pause, feche telefone ou troque de perfil. Saldo/perfil alterado deve impedir a confirmação antiga. Reabra e confirme nova operação válida.
6. Recarregue os plugins e confira os dois saldos no app e no F10. Teste com protagonista e personagem RPCore. Durante o comprovante, voltar não executa outra movimentação.
7. Confira navegação por mouse/setas/controle, contatos/configurações/busca, escala em sua resolução, celular em carro parado e fechamento ao começar a andar com o veículo. Confira inventário/atalhos e apps antigos.

Não há GTA disponível neste ambiente. Compilação e testes financeiros/de interface não comprovam o desenho e o input nativos; valide os passos acima no jogo.
