# Validação — RPCore 0.31.0

Build Release net48 com SHVDN Enhanced 1.1.0.6/LemonUI 2.2: zero erros/avisos. DLL SHA256 `9bacf37b59b6c65bbc23fcb04b75f3686982e3c6da75bf2c92b552c6ed6fde3c`.

1125 testes aprovados, zero falhas/ignorados: 1099 anteriores e 26 novos. Cobrem configuração de referência, save antigo sem propriedade/cobrança, compra/banco/ownership/snapshot, rollback/replay, limite de saldo/preço, campos desconhecidos e demais sistemas preservados, perfis independentes, piso/altura/normal inválidos, espera/cancelamento/timeout/movimento único/overflow. Relatórios DataTests.trx/DataTests-0.31.0.trx. Não executam GTA.

Campo aditivo Wallet.Housing com Properties/Receipts. Compra única do Eclipse por $1425000 padrão do banco RPCore, salva ownership e banco juntos; falha restaura original. Atualizar não concede imóvel nem cobra. Configuração Jobs/Housing.json nova; configurações anteriores intactas. Caixa empresarial, estoque, veículos, necessidades e progressos anteriores não são reescritos.

F10 Moradia e blip Safehouse, E na entrada/saída, menu/GPS/compra e entrada separada. Carregamento assíncrono de IPL/colisão; exige interior pronto, piso razoavelmente horizontal na altura esperada e ponto sem pedestre/veículo. Personagem fica no ponto original até verificação. Movimentação de 3m, pausa, morte, missão, modal, troca de perfil ou timeout cancelam. Atividade compartilhada bloqueia acesso concorrente enquanto carrega; sai sem nova cobrança. Saída disponível a perfil já dentro mesmo sem propriedade e com procurado, sem apagar estrelas. Sem congelamento/invulnerabilidade/fade persistente; não descarrega IPL compartilhado ao sair/encerrar.

Auditoria PSRP-SYSTEMS-AUDIT-0.31.0.json: 75 fontes, baseline 0.30.0, DLL atual, Apartments.OnLoad/OnTick/LoadHighEndApartment, preço/posições/headings/IPL conferidos. Não foram encontradas chamadas externas a Apartments.Instance nessa exportação; presença da classe não foi tratada como recurso ligado na referência. RPCore registra explicitamente seu módulo em Main. Sem guardarroupa/editor, storage interno, hotéis/aluguel, mobiliário, garagem grátis ou alterações às necessidades nesta subfase. Diferenças PDM continuam registradas; fases 8/9 preservadas.

GTA não executado nesta nuvem. Roteiro FASE-6.2A-0.31.0.md: blip/GPS/entrada, compra, banco, IPL/piso, entrada/saída, destino bloqueado, cancelamento, reload/perfis/procurado e regressão dos sistemas anteriores. Compatibilidade do apartamento/interior instalado precisa de teste real.

Instalador executado duas vezes via PowerShell 7/Linux sobre instalação simulada da 0.30.0: 111 arquivos existentes preservados byte a byte, incluindo saves/configurações/F2 e empresa. Housing.json acrescentado só na primeira execução; personalização conservada na segunda. Backup da DLL anterior e DLL instalada idêntica ao build. Relatório installer-validation-0.31.0.json. Não é teste de gameplay.
