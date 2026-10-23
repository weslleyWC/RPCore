# RPCore 0.12.1 — acesso ao trabalho de piloto

O usuário seguiu o GPS da 0.12.0, iniciou a qualificação e recebeu quatro estrelas ao entrar no LSIA. A base padrão estava numa área restrita e o contrato não fornecia acesso ao aeroporto. O relato não confirma o gatilho exato do encerramento: o runtime encerra por morte/missão/perda/dano da aeronave, não simplesmente pelo número de estrelas. A falha de acesso precisa ser corrigida independentemente disso.

## Como fazer agora

1. Feche o GTA e instale a atualização 0.12.1 da pasta extraída com seu caminho real do jogo, como nas versões anteriores. Não apague Pilot.json, INIs ou saves.
2. Saia da área restrita do LSIA e espere perder o procurado antes de contratar. O mod não apaga estrelas.
3. F10 → Trabalhos → Piloto → GPS: base de aviacao. O ícone agora fica no aeródromo McKenzie Field, em Grapeseed, com acesso livre no GTA padrão. O balcão fica no pátio (2118, 4796, 41.2). Não precisa comprar hangar para esta base.
4. Contrate Voo de qualificacao a pé, vá até o Velum azul, entre como piloto, pare na retirada e pressione E. Decole, mantenha voo a pelo menos 20 metros do chão e 15 m/s por dez segundos observados, e siga o GPS até Sandy Shores.
5. Pouse e pare no marcador em Sandy Shores; E confirma. A qualificação não paga dinheiro: salva a licença do perfil e libera os transportes comerciais. A aeronave ocupada continua disponível para você voltar à base; sair dela permite a recolha quando vazia/parada/em terra.
6. Depois da qualificação, contrate avião ou helicóptero na base de Grapeseed. Desembarque no destino paga a entrega; retorno vazio à base paga bônus. Motor X/tecla configurada ou menu religa após o desembarque. [Ciclo de voo/desembarque e controles](FASE-4.4-0.12.0.md) continua igual, substituindo as menções históricas ao LSIA pela base pública de Grapeseed.

## Alterações

- config/Jobs/Pilot.json: nova base pública própria. Start (2118, 4796, 41.2); PlaneSpawn (2130.8, 4806.2, 41.2), HeliSpawn (2111, 4791, 41.2), ambos direção 115. A busca existente testa vagas próximas, chão e colisão antes de criar. São coordenadas novas do RPCore; não são extraídas da PSRP. Precisam da verificação prática de espaço/manobra no seu GTA.
- Sandy Shores e os cinco destinos de helicóptero permanecem nos pontos da referência. Para a distância maior de Grapeseed até Arcadius, o prazo padrão executivo passa de 150 para 240 segundos. Valores personalizados e prazo desativado são preservados.
- FlightData.cs/PilotAccess: reconhece o conjunto exato das três posições/ângulos padrão LSIA da 0.12.0 e substitui só a base em memória. Não regrava o JSON. Preserva modelos, passageiros, valores, destino, helipontos e campos desconhecidos. Se qualquer ponto/ângulo da base já for personalizado, mantém o conjunto local. KeepLegacyLsiaBase=true permite conservar explicitamente a base antiga para instalações que já têm acesso ao LSIA; não concede acesso nem imunidade.
- PilotJob.cs: aplica a resolução em memória, remove nomes LSIA fixos dos menus/GPS/instruções e grava “Pilot access:” ao migrar. Se falhar por morte/missão/aeronave, grava “Pilot failure:” com existência/vida da aeronave e procurado para diagnóstico.
- SettingsSystem.cs: GPS renomeado para piloto/base de aviação.
- AssemblyInfo.cs/Main.cs/CoreMenu.cs/VisualAssetDefaults.cs: versão 0.12.1.
- tests/FlightTests.cs atualiza o default esperado; tests/PilotAccessTests.cs adiciona 11 casos da migração. scripts/audit-psrp-systems.py distingue a base nova dos destinos de referência. Documentação e pacotes atualizados.

Não há limpeza contínua de procurado, alteração de estatísticas de propriedade, compra automática de hangar, mudanças de relação com a polícia, teleporte do jogador ou exclusão de tráfego. A licença já salva e o progresso permanecem iguais. Celular F2 e os trabalhos anteriores não mudaram. Nenhum OIV/YTD/RPF/DLC precisa ser reinstalado.

## Validação e teste no GTA

Build Release net48 com SHVDN Enhanced 1.1.0.6/LemonUI SHVDN3 2.2: zero erros/avisos. 396 testes aprovados: 385 anteriores e 11 novos. Migração da base padrão sem alterar o objeto/JSON local, preservação de modelos/preços/destinos/unknowns, personalizações de pontos/ângulo, opt-in LSIA, prazo padrão versus customizado/desativado, configuração antiga sem flag e idempotência. A auditoria mantém os seis destinos de referência (Sandy e cinco helipontos) e identifica separadamente os três pontos próprios novos.

O acesso/voo ainda precisa ser testado no GTA. Confira que o GPS sai do LSIA, os dois modelos aparecem em vagas utilizáveis em Grapeseed e é possível decolar sem procurado causado pela área. Complete qualificação, avião com retorno e helicóptero com retorno; confira pagamentos, prazo Arcadius, F2 e saves. Se ainda falhar, envie “Pilot access:”, “Pilot aircraft spawn” e “Pilot failure:” do arquivo scripts/RPCore/RPCore.log. Não basta o teste de dados para validar cenário/nativos/IA.
