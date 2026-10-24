# Validação — RPCore 0.27.0

Build Release net48 com SHVDN Enhanced 1.1.0.6/LemonUI 2.2: zero erros/avisos. DLL SHA256 `6a7ee822e6225609727103b0423dc9e6f29e3d14f57f9d7e04b148639857a5d7`.

998 testes aprovados, zero falhas/ignorados: 965 anteriores e 33 novos. Cobrem configuração/mescla não destrutiva, contratação/salário/acerto da zeladoria, sujeira/cotação/limites/tempo, elegibilidade, serviços/pagamento atômicos, desconto da oficina, interrupção por dívida, cancelamento/recuperação/replay/rollback, campos desconhecidos, bloqueio entre operações e máquina de estados/overflow. Relatórios DataTests.trx/DataTests-0.27.0.trx. Não executam GTA.

Campos aditivos Pdm.PremisesService, DirtSeconds e Stock.Dirt, com estado vazio/limpo para saves anteriores. Sujeira passa a crescer apenas nos carros atribuídos ao showroom durante tempo ativo, salvo se desativada na nova configuração. Reduz novas cotações até 7,5%; contratos já fechados mantêm seus valores. Limpeza/reparo não remove estoque nem vaga. Reparo cobra somente na confirmação, limpeza não tem taxa adicional, salário continua devido. Falhas restauram estado e replay não duplica cobrança. Cancelamento antes da confirmação mantém condição e caixa.

Funcionários próprios com posições/modelos de referência, alternativa de ponto público livre, animação e esponja na limpeza. Sem invulnerabilidade dos funcionários, remoção de pedestres do ambiente, alterações globais de relações ou acesso forçado a interiores. Contratação persistente; NPC morto pode ser recriado após espera, sem nova contratação. Serviços interrompem por morte/procurado/missão/distância/tempo/funcionário indisponível ou salário atrasado. Menu/telefone/pausa impedem progresso do gesto. Só recursos próprios são limpos.

Auditoria PSRP-SYSTEMS-AUDIT-0.27.0.json: 75 fontes indexadas, baseline 0.26.0, identidade da DLL atual; SpawnMechanicPed/SpawnCleanerPed, posições, CreateCleanerSequence/animação e sujeira conferidos. Limpeza de carros solicitada e economia de sujeira são adaptações próprias. Limpeza de piso/lixo, IA de seguranças, reformas físicas e paridade completa da empresa permanecem pendentes. Fases 8/9 mantidas.

GTA não executado nesta nuvem. Roteiro FASE-6.1I-0.27.0.md: contratação e salários, chegada/caminho/animação de funcionários, sujeira visual e cotação, limpeza sem taxa, reparo com/sem oficina, cancelamento/morte/troca de perfil e regressão de showroom/vendas/financiamentos/retomada/locadora/coletas/garagem/F2/F11. Navegação/interiores/props exigem teste real.

Instalador executado duas vezes via PowerShell 7/Linux sobre instalação simulada da 0.26.0: 107 arquivos existentes preservados byte a byte, incluindo saves/configurações/F2 e empresa. PdmPremises.json acrescentado só na primeira execução; personalização conservada na segunda. Backup da DLL anterior e DLL instalada idêntica ao build. Relatório installer-validation-0.27.0.json. Não é teste de gameplay.
