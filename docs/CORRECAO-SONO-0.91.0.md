# 0.91.0 — correção do sono/desmaio

A0.90 derrubava o personagem novamente a cada30s, sem recuperar o sono. Comer/beber não resolvia o cansaço, e a cama nativa não atualizava o RPCore. Essa queda periódica foi removida.

Agora, com sono zerado, a pé e em condição segura, o personagem desmaia **uma vez por10s ativos**. Ao terminar, grava **sono100, fôlego100 e cooldown0**. Não cura vida nem enche fome/sede. Fome/sede zeradas ainda exigem alimento/água, mas não causam essa queda repetida.

Não inicia em veículo, embarque, água/ar/escalada/queda, combate/procurado/missão ou atividade em andamento. Se outro script colocar o jogador num veículo durante o episódio, suspende sem ejetar/teleportar ou mexer no carro; só retoma a pé em condição segura. Não bloqueia controles de corrida/Shift dentro de veículos. Hotkeys RPCore ficam suspensas durante a recuperação, sem desativar o pause nativo.

O progresso é opcional em Needs.json/SleepRecovery. Salva antes da queda e antes de conceder recuperação, com backup e preservação dos campos antigos. Recarga conserva tempo restante; falha na gravação final mantém episódio concluído para tentar salvar novamente, sem outra queda. Morte usa a recuperação hospitalar existente e encerra episódio válido. Recibo de comida pendente não reverte o sono recuperado para o valor antigo.

## Testar no GTA

1. Na situação atual de sono zerado, saia do veículo, feche menus e pare em local seguro fora de trabalho/missão/procurado. Deve cair uma vez, aguardar cerca10s e levantar com o ícone de sono/fôlego cheios. Corra e confirme que a queda não volta após30s. Recarregue e confira persistência.
2. Repita com sono zerado dentro de carro/moto, como motorista e passageiro. Não deve cair, ser ejetado, teleportado ou perder controles. Ao sair, ha3s de margem para terminar o desembarque; aguarde o desmaio em local seguro. Embarcar também bloqueia o começo.
3. Durma na cama nativa da casa antiga/nova do Franklin. O RPCore agora observa proximidade da casa, fade/perda nativa de controle e salto de4–12h do relógio. Ao voltar, deve encher sono/fôlego. Mudança manual de hora pelo RPCore não conta como dormir; sincronização do relógio cede enquanto a cama nativa controla o personagem.
4. Recarregue no meio do episódio: mantém tempo restante e retoma somente a pé/seguro; não ganha recuperação offline. Teste morte/troca de personagem e confira que não transfere recuperação entre perfis. Um ragdoll anterior pode terminar naturalmente antes da retomada; não há limpeza global de tarefas.

A cama é reconhecida por contexto/avanço do relógio, não por um evento oficial de sono do GTA. Camas/customizações de mapa ou scripts que não avançam o relógio podem não ser reconhecidos: nesses casos a recuperação automática por desmaio ainda encerra o problema do sono zerado. A reação física/tempo nativo e as camas precisam deste teste no GTA; os testes automáticos validam regras, tempo, persistência e exclusões.

F11/F2/radial, roleplay padrão, itens, dinheiro e os demais refinamentos0.90 preservados. Esta entrega é uma correção pontual antes de continuar a fase8.
