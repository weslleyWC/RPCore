# Revisão de paridade — base0.87.0

Auditoria estática, não homologação no GTA. O índice inclui75 arquivos exportados da referência. Classes/métodos encontrados não demonstram que o ciclo funciona. Este fechamento da lacuna de oficina não é fechamento de toda a revisão.

| Bloco | Evidência de referência e RPCore | Classificação / encaminhamento |
| --- | --- | --- |
| Oficina | PSRP/MechanicShop.cs, PopulateCustomsMenu/GetSlot*/pintura/rodas/extras; RPCore WorkshopSystem/WorkshopRules, garagem e callbacks do editor | Núcleo implementado0.87: compra/verificação/snapshot; teste GTA pendente. RGB livre, cor xenon, drift, previews, preços/câmera são diferenças explícitas para8/9 |
| Táxi/limousine de passageiro | PSRP/TaxiService.cs CallTaxi/CallLimo/SaveCurrentTaxi e TaxiRide.Update: chegada, embarque, viagem ao waypoint, cobrança e saída; nenhum serviço equivalente ligado em Main/PhoneSystem RPCore | **Sistema ausente**. Trabalho Uber não substitui contratar motorista. Próximo bloco funcional, com configuração/telefone e persistência segura |
| Clima/tempo | PSRP/WeatherSystem.cs: clima dinâmico, clima real, sincronizar relógio, lat/long/local e unidade; RPCore lê World.Weather no streamer, mas não tem controlador/menu equivalente | **Sistema ausente**. Agrupar controles de mundo na próxima entrega; recursos dependentes de rede precisam fallback explícito e opt-in |
| Editor | PSRP/MapBuilder.cs salva pontos/blips; RPCore MapEditorSystem acrescenta props/biblioteca/importação própria | Ciclo implementado0.86; não importa estrutura PSRP automaticamente. Diferença de formato documentada; sem migração destrutiva |
| Moradia/inventário/serviços | Auditoria anterior por housing_eclipse/wardrobe/home_storage/barber/grooming/cosmetics/face_editor/characters/vehicle_insurance/dmv e implementações ligadas em Main | Núcleos anteriores preservados; esta entrega não revalida cada imóvel/interior/aplicativo no GTA nem declara paridade total |
| Gangues/polícia | Blocos auditados6.x; contratos/territórios/assaltos, fiscalização/custódia/perseguição | Diferenças explícitas: reforços motorizados/variantes/co-líder, suborno/BOLO automático e integração opcional LSPDFR. Revisão ainda deve classificar cada variante; sem afirmar equivalência por nome de classe |
| HUD/menus/necessidades | F11/J/F2/inventário e ícones existentes | Fase8: aparência **e mecânicas**, incluindo fome/sede; não confundir ícones desenhados com efeito funcional |
| Trabalhos/missões | Ciclos anteriores e guias de teste | Fase9: revisão física/realismo comparando handlers PSRP; mensagens “próxima fase” não significam teste GTA aprovado |

Próximo bloco: transporte de passageiro e controles de mundo. Não há justificativa para repetir uma entrega por cada cor/variante. A lista acima é evidência da revisão atual; possíveis diferenças adicionais devem ser demonstradas pelo handler e sua integração, não por percentuais inventados.
