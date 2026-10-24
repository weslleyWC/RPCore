# Validação — RPCore 0.36.0

Build Release net48 com SHVDN Enhanced 1.1.0.6/LemonUI 2.2: zero erros/avisos. DLL SHA256 `e5b134c0ed2125941cd79aad82c87d6510eb3cf567647f48d516492321aa412c`.

1267 testes aprovados, zero falhas/ignorados: 1233 anteriores e 34 novos. Novos casos: IDs/paletas dos 11 overlays, IDs indevidos/duplicados, limites, compatibilidade com looks/pendências antigos, merge parcial, campos desconhecidos, snapshots, combinação com taxa única, reload/replay, falhas antes/depois da autorização e preservação de grupos. Relatórios DataTests.trx/DataTests-0.36.0.trx. Não executam GTA.

Serviço Grooming existente ampliado; nenhum novo arquivo de configuração. Overlays é coleção aditiva em Look/Pending.Look, ausente em saves antigos vira vazia. Valores anteriores de cabelo/barba/sobrancelhas conservados; nenhum auto-load de aparência nem modificação dos saves na instalação. Mesmo preço Grooming.json (120 por padrão), só grupos escolhidos; mesmos begin/start/complete, retomada/reaplicação gratuitas e estorno somente antes do início.

Native preflight exige freemode/rosto pronto, valida estilos e paletas atuais. SET_PED_HEAD_OVERLAY só grupos selecionados; tintura apenas nos overlays coloridos, tipo 2 maquiagem/blush/batom, tipo 1 pelos do peito. Amostras usam GET_PED_MAKEUP_TINT_COLOR para paleta de maquiagem e GET_PED_HAIR_TINT_COLOR para pelos. Getters de paleta não são tratados como cores atuais. Sem memória por offsets, troca de ped, head blend padrão, alteração de corte/roupa/tatuagem ou prévia impossível de reverter. Corpo pode estar oculto pela roupa; recursos nativos podem não responder igualmente a cores.

Auditoria PSRP-SYSTEMS-AUDIT-0.36.0.json: 75 fontes indexadas, baseline 0.35.0 e hash da DLL atual. Sete Apply de pele/maquiagem comparados; batom/peito/corpo são ampliações nativas próprias explicitamente registradas. Código próprio; nenhuma DLL da referência executada. Criador genético/morphs/ped/tatuagens e fases 8/9 continuam pendentes.

GTA não executado nesta nuvem. Roteiro FASE-6.3C-0.36.0.md cobre os 11 grupos, paletas/remoção/opacidade, cancelamento e interrupções, cobrança/retomada, dados antigos e regressões. Compilação e testes não comprovam renderização, coexistência com scripts de aparência ou gameplay.

Instalador executado duas vezes via PowerShell 7/Linux sobre instalação simulada da 0.35.0: 116 arquivos existentes preservados byte a byte. Inclui propriedade Eclipse, roupas, inventário/containers/layout, PDM/trabalhos, look Grooming anterior e campos desconhecidos, preço personalizado 200, configs/F2 e catálogo parcial/bindings inválido. Backup da DLL anterior e nova DLL idêntica ao build verificados. Sem alteração de .oiv/.rpf. Relatório installer-validation-0.36.0.json. Não é teste de gameplay.
