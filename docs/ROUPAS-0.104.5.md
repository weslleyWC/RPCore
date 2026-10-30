# Roupa atual e conjuntos persistentes — 0.104.5

O usuário confirmou que a troca de personagem da0.104.4 funciona e relatou que roupas não ficam salvas e conjuntos carregam vazios. Foram lidos Character.json, Wallet.json e RPCore.log enviados. **A troca de modelo Main.SwitchCharacter continua exatamente a da0.102.2**, sem seleção automática, candidato CREATE_PED ou CHANGE_PLAYER_PED.

## Evidência recebida

O cartão tem Body:null e ApplyPending:true. O modelo foi trocado, mas a aplicação não chegou ao checkpoint final que preencheria Body e liberaria o salvamento normal. O código anterior interrompia definitivamente em qualquer frame de câmera/contexto ou menu indisponível depois da troca, deixando a pendência. O log registra a troca0.104.4, mas não fornece o frame exato de interrupção.

A biblioteca recebida possui um conjunto válido estruturalmente, com os dez componentes Drawable0/Texture0 e os cinco props sem acessório. Portanto carregar esse registro aplica os índices padrão gravados, não os índices do kit personalizado descrito. Não há números desse kit no arquivo recebido para reconstruir suas peças. O arquivo original é preservado; se houver um Wallet.json.bak anterior com os valores corretos, pode conter outra versão. A atualização não inventa roupas, não troca o sexo/modelo e não substitui a biblioteca por dados vazios.

Barber e Grooming recebidos contêm aparência confirmada; a recuperação reaplica cabelo/olhos do modelo quando os recursos existem, além dos acabamentos de rosto existentes. Não publica esses arquivos pessoais no repositório; hashes e evidências sanitizadas em ANALISE-ROUPAS-0.104.5.json.

## Correções do carregamento

Antes de trocar o modelo, mantêm-se as condições antigas. Após a troca, o carregamento espera o rosto e o contexto voltarem a permitir aplicar a aparência, em vez de abandonar permanentemente por uma oscilação breve da câmera/menu. Identidade/modelo/perfil diferentes, cancelamento explícito ou prazo10s continuam interrompendo; não limpa ApplyPending só para ocultar falha. Não acrescenta demora inicial de30s ou novo fluxo de modelos.

Corpo/roupa/equipamentos são aplicados depois do head blend. Aparência confirmada e, quando mais recente, roupa confirmada são aplicadas antes da captura final. Só então grava Body e ApplyPending:false. O log passa a registrar character appearance committed ou o motivo/tempo de interrupção. Validação de drawable antecede consulta de textura, evitando consultar um índice de peça ausente.

## Roupa confirmada e biblioteca

Salvar roupa, vestir conjunto e concluir edição registram imediatamente a roupa confirmada do perfil/modelo, sem depender de esperar o autosave do corpo. O dado opcional fica em Wallet.Wardrobe.Equipped, uma entrada por modelo, com revisão crescente. Salvar um conjunto pela UI grava biblioteca e confirmação na mesma transação do Wallet; falha de IO restaura o estado anterior. Vestir recusa incompatibilidade antes de marcar como equipado e tenta restaurar a roupa anterior se o checkpoint falhar. Renomear/excluir um conjunto não desveste nem apaga a confirmação atual.

Character.BodyClothingRevision indica até qual confirmação a captura de corpo já incorporou. Assim, uma roupa recém-confirmada vence um Body antigo; depois de o corpo ser salvo novamente, uma confirmação antiga não força roupa obsoleta sobre alterações mais recentes. Campos opcionais preservam os JSON antigos e demais metadados. Banco, carteira, inventário, propriedades e outros conjuntos não são alterados pela transação de roupa.

Ao nomear um conjunto, a captura antes/depois da entrada de texto é comparada: se a roupa mudou durante o teclado do GTA, o código recusa substituir a biblioteca com essa nova aparência inesperada. O log inclui índices das peças em ações de salvar/vestir para verificar o resultado real, sem gravação por frame.

A referência PSRP.MultiplayerCharacter.SaveOutfitForCharacter/ApplyOutfit/ApplyLastOutfit_OnLoad foi revisada estaticamente: mantém roupa aplicada e a restaura após preparar o ped. O RPCore usa checkpoint/revisão próprios, sem copiar ped extra ou mudar o modelo. Sprites/OIV/RPF e roupas instaladas continuam os mesmos.

## Teste no GTA

1. Fechar o GTA e instalar0.104.5. Carregar pelo F10 a personagem existente e aguardar a mensagem Personagem carregado. Se estava Body:null/pendente, conferir depois no arquivo que Body existe e ApplyPending está false. Não editar essas flags à mão.
2. Montar um kit na loja, salvar com um nome novo ou usar **Substituir pela roupa atual** no conjunto antigo. O conjunto recebido estava com índices zero; para ele é necessário salvar novamente o kit montado, ou conferir um backup anterior que tenha os valores corretos.
3. Alterar a roupa, vestir o conjunto salvo e conferir todas as peças/acessórios. Voltar ao protagonista e carregar de novo: deve manter a confirmação correta. Repetir após fechar/reabrir o GTA, sem precisar esperar60s para gravar o kit.
4. Conferir que cabelo confirmado, rosto, inventário/arma/munição, saúde e saldos permanecem. Cancelar uma prévia não deve gravá-la; concluir edição deve conservar a roupa confirmada mesmo sem criar novo nome na biblioteca.
5. Se não concluir ou a roupa mudar inesperadamente, enviar RPCore.log e os novos Character.json/Wallet.json da tentativa. Não apagar a biblioteca/backup nem repetir crashes. Nenhum teste cloud executa as variações nativas do GTA.

3996 testes aprovados (3974 anteriores +22), Release net48 sem erros/avisos. Checkpoints/rollback/replay/revisões/perfis/metadata e política de interrupção cobertos no host; Main.SwitchCharacter comparado exatamente à base0.102.2 e demais mudanças delimitadas em CLOTHING-SCOPE-0.104.5.json. A confirmação visual e a conclusão real do rosto ainda precisam do teste GTA.
