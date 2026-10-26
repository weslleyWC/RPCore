# 0.82.0 — rendição, custódia e liberação

O sistema é independente do trabalho de transporte prisional. Desligado em saves antigos; ativação por perfil em **F11 → Polícia / trânsito e multas → Custódia / prisão / fiança**. PSRP.PrisonSystem foi lida inteira: ela suprime polícia globalmente, sem fichamento/pena/liberação. RPCore implementa seu próprio ciclo, integrado à perseguição nativa; não altera dispatch, limite máximo de estrelas, relações globais ou scripts de outros mods.

## Um roteiro de teste

1. Instale a atualização com GTA fechado, como as anteriores. Ligue **Custódia** no menu acima. Fiscalização de trânsito pode ficar desligada neste teste.
2. Durante uma perseguição nativa com 1–5 estrelas, saia do veículo, selecione desarmado e pare próximo de um policial vivo a pé (até15m, visão livre). F11 → Custódia → **Render-se a policial próximo**. O botão confirma rendição e transferência. Fique parado/desarmado durante3segundos; END cancela antes do fichamento.
3. O acesso à cela é verificado antes do fichamento. Se o interior/piso estiver indisponível ou ocupado, a operação recusa em10segundos sem limpar estrelas, cobrar ou gravar prisão. Mission Row padrão:459.85,-994.38,24.91; posição/raio/tempo/fiança em`Police/Custody.json`. Interiores substituídos por outros mods podem exigir ajuste.
4. Com fichamento salvo, apenas a perseguição entregue é encerrada uma vez. Você vai para a cela:60segundos ativos por estrela, fiança$500 por estrela. Não há confisco de armas/itens/dinheiro nem congelamento/imunidade. Polícia nativa ainda pode prender/matar durante a preparação; isso cancela a entrega, sem criar ficha artificial.
5. Feche menus para contar pena. Pause/menus/celular suspendem contagem. Checkpoints absolutos a cada5segundos e ao salvar/suspender; recarga não concede tempo offline, não recria policiais e não teleporta. Uma interrupção abrupta pode perder até5segundos de crédito ainda não gravado.
6. Teste **END**/recarga: o restante continua pendente. F11 → Custódia → **Retomar cumprimento da pena** confirma retorno à cela, a pé e sem novas estrelas/atividade. Sair do raio, morrer, iniciar missão ou ganhar novas estrelas interrompe a custódia; novas estrelas continuam nativas. Não há perseguição própria por fuga nesta versão.
7. Teste dois caminhos: **Concluir pena e sair** após zerar tempo (gratuito), ou **Pagar fiança e sair** dentro da cela (débito exato pelo banco, uma vez). Saldo insuficiente mantém tudo. Saída ocupada/sem piso não cobra. Multas de trânsito permanecem separadas. F11 pode mostrar restante salvo até5segundos acima do contador; liberação faz checkpoint antes de conferir.
8. Se uma falha ocorrer depois de gravar a liberação e antes de sair, **Recuperar saída já liberada** funciona apenas na cela do último processo, sem nova cobrança. Não permite teleporte a partir de qualquer lugar.
9. Confira troca de perfil, início dos empregos antigos após liberação/suspensão, celular F2, teleporte F11 e fiscalização0.81. Gate exclusivo bloqueia outras atividades durante cumprimento ativo. Suspender é ferramenta explícita de teste: conserva pena e não teleporta nem perdoa.

## Dados e limites

Wallet.Custody opcional, metadados futuros preservados, snapshot dos parâmetros por processo, histórico32 com totais vitalícios. Pagamento/baixa num único Wallet save com rollback e replay protegido. Configuração inválida é conservada e usa defaults em memória; save inválido é protegido. Não há alteração de OIV/RPF/YTD ou saves/configs anteriores.

3070 testes de dados/transações aprovados, build Release net48 sem erros/avisos. GTA não executado neste ambiente: interior/câmera/colisão, interação com polícia nativa, menus e compatibilidade com outros controladores precisam deste teste. LSPDFR não é detectado/integrado automaticamente. Perseguição independente/escalada e infrações restantes seguem como diferenças funcionais; fase7 ainda pendente e8/9 reservadas.
