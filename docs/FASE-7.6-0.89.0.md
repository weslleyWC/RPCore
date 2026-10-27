# 0.89.0 — suborno, BOLO e contato com co-líder

## Testar a polícia

1. Abra **F11 → Polícia / trânsito**, ative fiscalização e, se quiser testar, **Suborno** e **BOLO por fuga**. As duas opções novas começam desligadas em saves antigos. Encerre abordagem pendente antes de alterar preferências.
2. Solicite patrulha observadora, cometa uma infração real e pare como motorista. Aguarde o agente próprio chegar a pé ao lado do carro, até2,5m. Durante a conferência, **E oferece suborno**; também há a opção no F11 nessa condição. Não funciona contra policial ambiente nem de longe/depois da abordagem.
3. A proposta custa **1,5× a multa**, com arredondamento da referência, exigido no banco. Só cobra se aceita. Chance-base60%; arma apontada ou BOLO na abordagem reduz para15%; total a partir de$300 reduz a chance em25%. Existe uma tentativa salva por autuação, sem nova rolagem após recarregar. Sem saldo suficiente, não registra tentativa.
4. Aceito: confira débito e autuação com **ACORDO**, recibo e pagamento desativado. Outras multas permanecem. Recusado: não cobra proposta, conserva multa e gera BOLO se habilitado. **Até2 estrelas nativas somente se Perseguição/reforço já estiver habilitado**, respeitando máximo do jogo; não apaga/reduz estrelas. Recarregar não reaplica o pedido.
5. Com BOLO ativado, fugir da abordagem registra modelo+placa. **END, recarga e interrupção não geram BOLO de fuga**. Consulte a lista no F11. Não há multa automática por portar BOLO nem tomada de policiais/viaturas do ambiente.
6. Quite/resolva as autuações que originaram o registro. Selecione o BOLO no F11 para regularizar fora de abordagem/atividade/procurado/missão. Sem taxa extra; não remove estrelas nem mexe no seguro/cadastro. Antes de resolver a origem, deve recusar. Outro carro do mesmo modelo com placa diferente não compartilha o registro.
7. Recarregue com acordo, recusa, BOLO ativo e regularizado; confira saldo, multa e ausência de efeito repetido. Teste recuperação pelo seguro: a consulta usa modelo+placa, mantendo o seguro independente. Até256autuações/256registros; listas cheias não descartam multas não resolvidas.

Banco é escolha própria para salvar débito/recibo juntos. Aceito resolve a multa atual sem marcá-la como pagamento normal; estatísticas de multas pagas continuam representando os pagamentos exatos. Acordos ficam no histórico limitado, não são novo total vitalício. Falha normal de gravação reverte livro/banco; arquivo e efeito nativo não são transação resistente a encerramento abrupto.

## Testar co-líder

Com presença ligada, uma definição **sem LeaderModel** e com **CoLeaderModel válido** usa o co-líder como contato nas conversas/iniciações existentes. Usa o posto de líder configurado; se faltar, tenta HQ+1,5m emX, sujeito à validação de espaço/piso/modelo. Originais não são regravados. Não substitui líder configurado por morte/afastamento nem inventa NPC quando ambos os modelos faltam. Teste aproximaçãoE, início/retomada/entrega da iniciação e troca de perfil.

GTA não executado aqui. Compilação/testes de dados não homologam NPCs/modelos instalados. FECHAMENTO-BASE-0.89.0.md registra o próximo bloco: fase8 de interface **e mecânicas de necessidades**.
