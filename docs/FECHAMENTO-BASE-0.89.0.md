# Fechamento da implementação-base — 0.89.0

A fase7 encerra o inventário da base funcional independente. **Não significa equivalência exata com a PSRP nem homologação no GTA.** Os guias pendentes continuam válidos; “próxima fase” não foi contado como aprovação de gameplay.

| Diferença conhecida | Evidência / decisão concreta | Destino |
| --- | --- | --- |
| Suborno/BOLO | BetterPoliceSystem.OfferBribe/SetWanted e Vehicle_Insurance.GiveBoloToVehicle; TrafficSystem/PoliceResolutionTransactions executam tentativa/recibo/débito/rejeição/registro/regularização | Implementado0.89; teste GTA pendente. Banco, opt-in e placa+modelo são diferenças deliberadas |
| HasBolo como infração | Enum e severidade em OfferBribe, sem check acrescentando a infração no export BetterPoliceSystem | Não inventar multa automática; registro funcional agrava risco do suborno |
| Co-líder | GangSpawner.BeginSpawn escolhe CoLeaderModel se LeaderModel vazio; GangPresenceRules.ContactModel/Plan alimentam presença/contatos existentes | Implementado0.89 para co-líder configurado. Sem substituir por morte ou inventar fallback em saves antigos |
| Roubos móveis/reforços motorizados | GangSpawner possui veículos de reforço; RPCore possui captura/defesa/reforços a pé, comboio, drive-by e contratos de roubo/suprimentos/armas | **Variantes físicas ainda não implementadas**, atribuídas à fase9. Não equivalem a alvos fixos |
| Tuning ligado à gangue | Oficina0.87 personaliza veículos; contratos de mecânico têm ciclo próprio. Papel do tuning na gangue não equivale a abrir a oficina | Variante/interação de atividade na9; opções visuais adicionais também descritas no guia0.87 |
| LSPDFR/painel do policial | Classes da referência dependem de integração externa diferente de SHVDN+LemonUI | **Integração opcional não implementada**, fora desta base; não exigir/instalar RagePluginHook/LSPDFR |
| Transporte/mundo | PassengerSystem/WorldSystem0.88: táxi/limousine, reserva/recibos e clima/hora opcionais | Núcleos implementados; GTA/IA/HTTP online pendentes. Sem teleporte de corrida ou HTTP síncrono |
| Editor/missões/moradia/serviços/apps | Guias e auditorias até0.88; editor/estúdio/Halloween, moradia/roupa/armazenamento/cuidados/personagens/seguro/DMV, inventário/telefone/rádio/bolsa têm caminhos RPCore | Preservar na regressão. Indexar75arquivos/classes não prova reprodução de cada detalhe |

## Próximas fases

**Fase8 começa na próxima entrega:** corrigir/refinar fome/sede/sono/vida/armadura e apresentação; HUD, F11, radialJ, F2, inventário e demais menus. O usuário especificou que os ícones existem: o efeito mecânico também precisa funcionar. Usar evidência PSRP e verificar persistência/consumo/tempo/pausa.

**Fase9:** todos os trabalhos, contratos, atividades de gangue e missões; estágios, veículos/NPCs/alvos móveis/reforços, animações, dificuldade/pagamento, risco/falha/apresentação. Agrupar as variantes da tabela, sem afirmar que já existem. Não apagar essas pendências ao fechar a base nem reabrir uma entrega-base por variante.

Conversão automática de MapBuilderData/saves PSRP, portas ambientais, invencibilidade, supressão global de polícia, takeover de trânsito e câmeras forçadas não foram copiados. Diferenças explícitas da implementação própria/preservação dos recursos instalados; não prova de paridade total.
