# Pendências antes dos refinamentos — base0.84.0

A fila agrupa sistemas relacionados. Não representa uma atualização por item; tamanho das entregas depende da implementação e dos testes. Diferenças de um sistema que já tem ciclo funcional vão para o refinamento correspondente, sem reabrir indefinidamente sua fase.

| Bloco | Já entregue | Ausência funcional a concluir/verificar |
| --- | --- | --- |
| Gangues / fase6 | Presença, equipe, iniciações, captura/defesa, economia/estoques/armas, relações,15atividades;0.78 acrescenta declaração, alertas automáticos, simulação rival e reforços a pé;0.79 entrega comboio físico de quatro carros/assalto organizado;0.80 entrega emboscadas rivais e conversa presencial com líderes/iniciações/gestão | Blocos de emboscada/conversa desta etapa entregues; variantes móveis de roubo/reforço motorizado, tuning, fallback de co-líder e diferenças físicas seguem na revisão final/refinamento, sem declarar paridade total |
| Polícia/prisão / fase6 | Transporte prisional como trabalho;0.81:fiscalização7infrações/162limites, patrulha própria, abordagem, autuações/pagamento; polícia nativa preservada;0.82:custódia/fichamento/pena/fiança/liberação integrados à perseguição nativa;0.83:quatro infrações restantes, perseguição própria carro/pé e reforço nativo opcional | Núcleo independente desta entrega concluído; teste físico GTA pendente. Suborno/BOLO automático e painel/integração opcionalLSPDFR seguem como diferenças explícitas para revisão final, sem declarar paridade total |
| Missões/editor / fase7 | Infraestrutura de atividades, GPS e configurações próprias;0.84:The Hollywood Act(cinco preparações/final, persistência/prêmio/retomada) | Teste GTA dos seis atos; missãoHalloween, editor de objetos/mapa persistente com limpeza de recursos próprios |
| Revisão final / fase7 | Auditorias estáticas e testes de cada entrega | Lista final por sistema: funcionalidade ausente versus diferença deliberada versus refinamento; conferir moradia/serviços/apps/mundo e regressão |
| Refinamento / fase8 | Reservado pelo usuário | HUD/menus e mecânicas, incluindo fome/sede; F11/radialJ/F2/inventário e demais interfaces |
| Refinamento / fase9 | Reservado pelo usuário | Todos os trabalhos/atividades, comparaçãoPSRP, etapas/realismo/NPCs/veículos/animações/riscos e falhas |

A0.84 não declara paridade total das gangues nem antecipa8/9. Câmeras/apresentação, ajuste de duração/preço, detalhes físicos e variantes de atividades com ciclo já implementado ficam explicitamente nos refinamentos; sistemas inteiros ausentes seguem na revisão de paridade. Não há percentual ou prazo garantido sem inventário final validado.

Próximo bloco de implementação: fase7 — Halloween, editor persistente e revisão final; assalto ao estúdio entregue em0.84, com teste GTA pendente. O fechamento do núcleo policial não declara teste GTA aprovado, equivalência total da PSRP ou implementação da integração opcionalLSPDFR.
