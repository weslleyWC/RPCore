# Validação — RPCore 0.10.1

Build Release net48 com SHVDN Enhanced 1.1.0.6/LemonUI SHVDN3 2.2: zero erros/avisos. DLL SHA256 `e81cf6271910c94f42f991f60f4d42c59b7721aa38f9875fc61520006e222f97`.

309 testes aprovados, zero falhas/ignorados: 295 anteriores e 14 novos. Geometria de limites orientados (incluindo modelos assimétricos), margem/altura, veículo/pedestre ao lado dentro do antigo raio mas fora da vaga, colisão real no espaço, seleção da próxima vaga, todas bloqueadas, busca limitada a 35 pares, distância do par e preservação do ponto configurado. Evidências DataTests.trx/DataTests-0.10.1.trx. A geometria e a seleção dos testes são usadas pelo runtime.

Inspeção/compilação da integração nativa: dimensões Hauler/Tanker, entidades próximas, chão no centro/cantos, ajuste pela parte inferior do modelo e sondagens síncronas de paredes/cenário em três alturas. Duas vagas aprovadas antes de criar qualquer veículo. Não limpa veículos/peds/objetos para forçar spawn. Diagnóstico LS GAS spawn no RPCore.log com rejeições por ocupação/chão/cenário. Sondagens não foram executadas nesta nuvem: exigem teste no GTA com os recursos instalados.

Auditoria estática PSRP-SYSTEMS-AUDIT-0.10.1.json: 75 fontes de referência, binário atual e pacote 0.10.0 como baseline. Pontos configurados permanecem idênticos aos da 0.10.0; a posição original do tanque é tentada primeiro. A seleção pode deslocar o tanque até 40 metros para uma vaga próxima livre. Não regrava a configuração do usuário. Nenhum OIV/YTD/RPF/DLC/dlclist foi alterado.

O usuário confirmou cinco entregas/pagamento de reposição na 0.9.0 e os quatro contratos anteriores. A 0.10.0 foi testada e bloqueou a contratação com pátio ocupado, portanto não há aceite de engate/descarga/retorno LS GAS. Esta correção aguarda teste no mesmo local do print e um contrato completo; roteiro em CORRECAO-0.10.1.md.

Engate, estados de descarga, pagamentos/recibos/bônus, recolha, telefone F2 e formatos de save continuam os da 0.10.0, cobertos pelos testes anteriores. Turnos ativos não retomam após reload. A validação nativa no jogo continua necessária, conforme FASE-4.2-0.10.0.md.

Instalador testado duas vezes com PowerShell 7/Linux sobre GTA simulado com pacote 0.10.0: 91 arquivos existentes preservados byte a byte, incluindo FuelDelivery.json, saves e telefone F2. Personalização do JSON preservada na segunda execução, backup da DLL anterior e DLL instalada igual ao build. Evidência installer-validation-0.10.1.json. A simulação não executa GTA/Windows real.
