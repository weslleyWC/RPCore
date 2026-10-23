# Validação — RPCore 0.9.0

Build Release net48 com SHVDN Enhanced 1.1.0.6 e LemonUI SHVDN3 2.2: zero erros e zero avisos. DLL SHA256 `fe6c98920bc2ba85e14da654930b14f6a969abd8d11beea907583d08b60c5b54`.

256 testes aprovados, zero falhas/ignorados: 226 anteriores e 30 novos. Novos casos cobrem carregamento obrigatório, compatibilidade de estados antigos, cinco descargas e bônus único, recuperação de caixa, falha de pagamento/retry, cancelamento, progresso anterior/unknowns, defaults embutidos, seleção de rota sem duplicatas, reserva local das lojas, conflito de teclas e exclusão entre cinco contratos. Evidências DataTests.trx e DataTests-0.9.0.trx. São testes da lógica usada no runtime, sem simular GTA/nativos/IA.

Auditoria estática PSRP-SYSTEMS-AUDIT-0.9.0.json compara a PSRP.dll com o binário 0.9.0 e o pacote 0.8.0 como baseline, indexando 75 fontes de referência. Os 20 pontos do contrato de reposição e seu depósito aparecem em JobDeliveryDriver.cs da referência; lojas/Post OP/Uber/valet anteriores também mantêm os pontos verificados. StoreDelivery.json está embutido na DLL. Não há alteração em OIV/YTD/RPF/DLC/dlclist ou transplante dos fontes decompilados.

O usuário confirmou pizza/Post OP e seus pagamentos na 0.7.0 e Uber/valet na 0.8.0. A 0.9.0 não foi executada no GTA nesta nuvem. Precisam de teste real: spawn/portas/traseira do Mule2, carregamento, animação/prop, interação pública das lojas, prioridade de E, crédito por descarga, retorno/recolha, cancelamento/morte/perda de veículo/perfil e regressão dos quatro contratos anteriores. Roteiro em FASE-4.1-0.9.0.md.

Créditos, recibos e progresso de reposição usam o checkpoint bancário existente no Wallet.json por perfil. Cancelamento mantém entregas pagas; bônus só após todas as entregas e retorno. Sessões ativas não retomam após reload. Inventário pessoal e formatos anteriores são mantidos. As limitações de atomicidade entre JSON e dinheiro nativo nos sistemas anteriores permanecem descritas na validação 0.7.0.

Telefone F2 local preservado; conflito de garagem resolvido só em memória. JSON local válido mantém overrides/unknowns; inválido é preservado e usa fallback. Arquivos de defaults são criados apenas quando ausentes. A reserva da loja é limitada ao destino ativo e liberada ao avançar/terminar/cancelar/falhar. Recursos removidos pertencem ao contrato; não são removidos carros ou NPCs de ambiente para abrir espaço.

Instalador executado duas vezes com PowerShell 7/Linux sobre pasta GTA simulada com a distribuição 0.8.0: 89 arquivos existentes preservados byte a byte, incluindo saves, inventário e telefone F2. StoreDelivery.json foi instalado na primeira execução e sua personalização foi preservada na segunda. INI antigo de garagem F2 permaneceu idêntico; a resolução em memória é coberta pelos testes de teclas. DLL anterior em backup e DLL instalada idêntica ao build. Evidência installer-validation-0.9.0.json. Esta simulação não executa GTA nem valida instalação real no Windows.
