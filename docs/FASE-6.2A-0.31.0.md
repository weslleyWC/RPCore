# RPCore 0.31.0 — fase 6.2A: moradia / Eclipse Towers

## Instalação

Extraia RPCore-update-0.31.0.zip e abra o PowerShell na pasta com Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Ajuste o caminho. GTA V Legacy, SHVDN Enhanced 1.1.0.6 e LemonUI 2.2. Não execute PSRP.dll como segundo script; é referência de análise. Não precisa reinstalar .oiv/.rpf. Instalador conserva saves/configurações e acrescenta Jobs/Housing.json se ausente. F2 celular, F11 teleporte e sistemas anteriores mantidos.

## Como usar

F10 → Moradia / Eclipse Towers → GPS. Também há ícone de casa no mapa e interação E na entrada (-773.2661, 311.4976, 85.6981). Vá a pé, sem procurado/missão/combate e sem atividade em andamento.

Compre por $1425000 do banco RPCore. É o banco do mod; dinheiro em mãos do protagonista é outro saldo. Compra é única e persistente por perfil, sem aluguel recorrente. Não concede garagem/veículo. Depois da compra, escolha Entrar no apartamento. A compra não teleporta automaticamente.

O módulo solicita o IPL apa_v_mp_h_01_c, aguarda interior/piso e verifica espaço antes de mover você. Interior: (-786.8663, 315.7642, 187.9136), heading 269.61. Aguarde parado até 15 segundos; End cancela. Afastar 3m, abrir outra interface, pausar, morrer, iniciar missão ou trocar perfil cancela sem mexer na propriedade ou cobrar novamente. Interior indisponível/piso ausente mantém posição original; tente novamente após streaming carregar.

Para sair, volte ao círculo junto à porta interna e E, ou use F10 → Moradia → Sair. Destino público (-773.6778, 308.4663, 85.69805), heading 180.4528. Saída não exige que o perfil atual seja proprietário e permite procurado, mantendo as estrelas. Carro/personagem em missão não são teleportados. Não há invulnerabilidade, congelamento persistente, remoção de NPCs ou IPL global desligado ao sair.

## Roteiro no GTA

1. Confira F10 → Moradia e ícone/GPS no mapa. Na entrada, E abre o menu. Antes de comprar, Entrar não deve conceder acesso. Saldo insuficiente não deve cobrar/criar propriedade.
2. Com saldo suficiente no banco RPCore, compre. Confira débito único, propriedade adquirida e ausência de alteração no caixa PDM/garagem/veículos pessoais. Reabra/reinicie: continua adquirido e o valor pago permanece salvo.
3. Escolha Entrar e aguarde. Confira piso, posição e orientação. Ande pelo apartamento e volte à porta: E deve levar à saída pública. Repita entrada/saída e confirme que não cobra novamente.
4. Durante carregamento, use End, mova-se mais de 3m, abra celular/F11 ou pause. Deve cancelar/liberar atividade e conservar posição. Se interior/saída estiver indisponível ou ocupado, deve recusar mover em vez de colocar personagem sem piso. Reinicie o acesso após desobstruir/carregar.
5. Troque de perfil dentro do apartamento: o novo perfil não recebe propriedade, mas pode sair pelo círculo/menu. Propriedade continua com o comprador. Ao reiniciar scripts dentro, a saída deve continuar disponível; o sistema não salva nem força posição de respawn.
6. Confira saída com procurado se essa situação acontecer: não deve apagar estrelas. Entrada com procurado deve recusar. Depois teste F2, inventário, radial J, F11/teleporte, trabalhos e todos os módulos PDM.

Para teste com orçamento menor, opcionalmente edite Price em Jobs/Housing.json com o jogo fechado antes da primeira compra. Valor pago é snapshot; restaurar preço não cobra diferença nem permite segunda compra. Não precisa apagar saves. O padrão distribuído permanece exatamente $1425000.

## Escopo e referência

Analisada toda a classe Apartments da PSRP.dll: compra/entrada/saída/guardarroupa e loader do interior. Preço, coordenadas, headings e IPL vêm dessa referência. Não foram localizadas chamadas externas a Apartments.Instance nos 75 arquivos exportados, portanto não se afirma que a classe estava ligada no jogo de referência. O RPCore registra seu módulo próprio em Main, com compra atômica, carregamento assíncrono e verificação do piso.

Esta subfase entrega aquisição/acesso Eclipse. Guardarroupa/editor, armazenamento do apartamento, hotéis/aluguel, mobiliário e outros imóveis continuam para próximas subentregas/refinamentos. Não altera fome/sede/sono; revisão dessas mecânicas continua na fase 8. Diferenças PDM permanecem registradas para continuidade/refinamento, sem declarar paridade total. Fase 9 preservada.

Build/testes em VALIDACAO.md; comparação em PSRP-SYSTEMS-AUDIT-0.31.0.json. GTA não executado nesta nuvem: interior e colisão precisam do teste com seus recursos instalados.
