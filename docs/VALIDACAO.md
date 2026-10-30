# Validação0.106.1 — acesso Motors e menus PDM

4105 testes aprovados (4079 anteriores +26), zero falhas/ignorados. Release net48 sem erros/avisos. DataTests-0.106.1.trx. Não executa GTA/natives.

22novos casos de limites/modelos/entradas/fachada/piso/NaN/inf e isolamento de lojas alheias.4cenários de fila entre menus: ação adiada, tecla segurada ao abrir câmera, falha sem repetição/bloqueio de outro menu, ação descartada ao fechar/trocar página/perfil.

450arquivos fonte/config fora do escopo idênticos à0.106.0/e026668. Os12arquivos PDM também conferem ao remover somente mudanças de despacho/limpeza da interface e diagnóstico da abertura: regras/ciclos de negócio Tick/Save, preços, pagamentos e dados permanecem iguais. Main muda versão e protege limpeza do catch de despacho; Visuals isola falha de skin. Seguro/compra de veículo pessoal, Wallet, personagens/roupas e banco/combustível/cinto intactos. DEALERSHIP-ACCESS-PDM-SCOPE-0.106.1.json.

Instalador duas vezes sobre0.106.0 com PowerShell7/GTA simulado: 252 arquivos preservados byte a byte. Inclui saves/roupas/inventário/carteira/banco, catálogo/garagens personalizados, apólices e pedidos de seguro, configurações e backups. DLL nova confere, anterior em backup e RPF externo intacto. Relatório installer-validation-0.106.1.json. Conferência final compara111 payloads JSON/INI e áudios anteriores, CRC/manifestos, fonte, DLL, auditoria e TRX; não executa gameplay.

Referência estática MultiplayerCharacter.UnlockDoors e PDMCompany shr_int/fakeint. A fila usada é a dos serviços pessoais atuais; SDK LemonUI2.2 decompilado para conferir o ciclo ObjectPool.Process/Activated/ProcessControls. Não houve reprodução nativa do desaparecimento de HUD/menus relatado: esta entrega corrige riscos concretos, mas confirmação depende do GTA/logs. Roteiro PORTAS-PDM-MENUS-0.106.1.md.
