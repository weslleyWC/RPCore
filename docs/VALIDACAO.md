# Validação — RPCore 0.28.0

Build Release net48 com SHVDN Enhanced 1.1.0.6/LemonUI 2.2: zero erros/avisos. DLL SHA256 `5375271b6ca2f3e9183adf759480fb4924592c5a83cc365f994762711ff5a2fc`.

1035 testes aprovados, zero falhas/ignorados: 998 anteriores e 37 novos. Cobrem dois postos/configuração de referência, mescla não destrutiva de quatro/cinco/sete cargos, contratação/pagamento/acerto/rollback/replay, salários independentes e snapshots, bloqueio por dívida, ausência de bônus financeiro dos guardas, campos desconhecidos e critérios de seleção/exclusão de alvos/limites de distância. Relatórios DataTests.trx/DataTests-0.28.0.trx. Não executam GTA nem comprovam IA nativa.

Cargos opcionais security1/security2 na estrutura persistente de equipe já existente. Nenhuma contratação/cobrança automática, nenhum novo contrato físico de entrega, estoque ou veículo pessoal. Oferta é mesclada em memória sem regravar PdmStaff.json/PdmPremises.json. Guardas usam os mesmos checkpoints de tempo ativo, salários individuais e acerto proporcional. Sem salário offline ou débito automático no banco pessoal. Sem bônus de vendas ou oficina por contratar segurança.

NPCs próprios nos postos da referência, ponto seguro livre próximo até seis metros, blips de serviço, CarbineRifle com 120 munições sem soltar arma na morte. Sem invulnerabilidade, exclusão de pedestres do ambiente ou grupos/relações globais. Salário/acerto atrasado desarma e manda ao posto. Morte conserva contratação/salário e aguarda 60 segundos para recriar recurso próprio, conforme ponto disponível. Afastamento/missão/cutscene/procurado/morte do personagem recolhem os guardas próprios.

Defesa solicita TASK_COMBAT_PED apenas para NPC humano vivo, ambiente, com linha de visão e em combate contra jogador/equipe; exclui jogador, equipe, polícia/SWAT/exército e entidade de missão. Alvo e guarda precisam ficar no raio do posto, padrão 20m. Reavalia a cada 500ms; engajamento limitado a 20 segundos e retorno a pé com espera de dez segundos. Recall interrompe tarefa sem teleportar. Não cria ataques, remove estrelas, altera inimigos nem substitui polícia. Caminho/linha de visão/combate dependem do GTA.

Auditoria PSRP-SYSTEMS-AUDIT-0.28.0.json: 75 fontes indexadas, baseline 0.27.0, identidade da DLL atual, SpawnSecurity1Ped/SpawnSecurity2Ped, duas posições/headings, armas, defesa de área e folha de segurança conferidas. Defesa local seletiva, preços/tempo e salários são adaptações próprias. Seguros/roubos/CCTV, reformas e outros subsistemas da empresa não estão concluídos. Fases 8/9 mantidas.

GTA não executado nesta nuvem. Roteiro FASE-6.1J-0.28.0.md: contratar/pagar/demitir, chegada/postos/arma, retorno, dívida e pagamento, ataque de NPC ambiente e limites de defesa, suspensão com procurado/missão, morte/troca de perfil e regressão dos sistemas anteriores. Nenhum resultado de combate nativo é inferido dos testes puros.

Instalador executado duas vezes via PowerShell 7/Linux sobre instalação simulada da 0.27.0: 108 arquivos existentes preservados byte a byte, incluindo saves/configurações/F2 e empresa. PdmSecurity.json acrescentado só na primeira execução; personalização conservada na segunda. Backup da DLL anterior e DLL instalada idêntica ao build. Relatório installer-validation-0.28.0.json. Não é teste de gameplay.
