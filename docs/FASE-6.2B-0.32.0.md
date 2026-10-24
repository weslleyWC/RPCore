# RPCore 0.32.0 — fase 6.2B: guardarroupa e roupas

## Instalação

Extraia RPCore-update-0.32.0.zip e abra o PowerShell na pasta com Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Ajuste o caminho. Requer GTA V Legacy, SHVDN Enhanced 1.1.0.6 e LemonUI 2.2. Não execute PSRP.dll como segundo script. Sem reinstalar .oiv/.rpf. Compra do Eclipse/saves/configurações existentes preservados; adiciona Jobs/Wardrobe.json se ausente. F2, F11 e sistemas anteriores mantidos.

## Acesso

Entre no Eclipse adquirido na fase anterior. Vá ao guardarroupa no andar superior, ponto (-797.761, 328.302, 190.7137), e pressione E. Também há F10 → Moradia / Eclipse Towers → Guardarroupa / roupas salvas, disponível ao se aproximar do ponto no seu apartamento. Exige personagem a pé, fora de missão/combate/procurado e sem atividade concorrente. Perfil sem propriedade pode sair do apartamento, mas não usar esse guardarroupa.

## Biblioteca e editor

Salvar roupa atual pede nome de até 32 caracteres e grava roupa/acessórios do ped atual. Não cobra dinheiro. Limite padrão: 30 conjuntos por perfil, contando todos os modelos. Não sobrescreve nome repetido para o mesmo modelo silenciosamente: abra o conjunto e use Substituir pela roupa atual. Mesmo nome para outro modelo é permitido.

Abra um conjunto para Vestir, Substituir, Renomear ou Excluir. Excluir pede confirmação e não despe o personagem. Vestir exige o mesmo modelo e todas as variações disponíveis no GTA atual; recusa conjunto incompatível sem trocar seu modelo. Peças são validadas antes da aplicação e uma falha nativa tenta restaurar a roupa anterior. Biblioteca é persistente; vestir é manual, sem reaplicação automática ao iniciar scripts/trocar perfil.

Editar pecas e acessorios inicia uma prévia reversível. Escolha máscara, torso/braços, calças, bolsa, sapatos, acessórios, camiseta, colete, decalques ou blusa/jaqueta; ou chapéu, óculos, orelhas, relógio/pulseira. Use Peca anterior/Proxima peca e Textura anterior/Proxima textura. Acessórios incluem a opção sem acessório (-1). Mudança de peça reinicia textura/paleta em zero; captura de roupa existente conserva paleta.

Salvar roupa editada grava um conjunto novo e mantém a roupa atual. Concluir edicao mantém a prévia vestida, sem criar conjunto automaticamente; salve depois se quiser biblioteca. Cancelar e restaurar, Esc/fechar o menu, sair do local ou abrir outra interface cancela a prévia e restaura o backup do mesmo ped/modelo. Use Voltar as pecas para continuar editando sem cancelar. Trocar para outro ped/modelo cancela sem aplicar o backup nele.

O editor usa as variações do modelo atual e a câmera normal do GTA. Não muda cabeça, cabelo, rosto, modelo, tatuagens, vida, armadura numérica ou dinheiro. Peças do mesmo modelo podem precisar de combinação manual de torso/camiseta/blusa para não recortar o corpo; ajuste e salve o conjunto escolhido. Alterar mods/DLC de roupa pode mudar índices; conjunto com índices indisponíveis é recusado, mas índice ainda válido pode representar outra peça após mudanças dos recursos.

## Roteiro no GTA

1. Atualize e confirme que a propriedade Eclipse e o banco continuam iguais. Vá ao guardarroupa no andar superior e abra por E/menu. Fora do apartamento/ponto, deve orientar a ir ao local; não deve mudar roupa automaticamente ao carregar scripts.
2. Salve um conjunto atual. Abra editor e mude camiseta/blusa/calça/sapato e um acessório. Cancele: deve restaurar a roupa e acessórios anteriores, incluindo remoção/retorno de chapéu ou óculos. Cabeça/cabelo/rosto e modelo permanecem.
3. Edite novamente, escolha Salvar roupa editada e nome novo. Vista o conjunto anterior e o novo, conferindo peças/texturas/paleta/acessórios. Reinicie: biblioteca continua; use Vestir manualmente, sem cobrança.
4. Tente salvar nome igual com diferença de maiúsculas: deve recusar. Use Substituir pela roupa atual no conjunto escolhido para atualizar explicitamente. Renomeie; tente nome de outro conjunto do mesmo modelo; exclusão deve pedir confirmação. Cancelar exclusão conserva item, confirmar remove só da biblioteca.
5. Durante prévia, feche menu, abra F2/F11, afaste-se, morra ou troque perfil/modelo: deve cancelar/liberar atividade e restaurar apenas se ainda for o mesmo ped/modelo. Concluir edicao mantém roupa vestida sem criar conjunto. Confira que pode iniciar trabalhos após sair do editor.
6. Se o seu perfil for utilizado com outro modelo de ped, o conjunto aparece identificado como outro modelo e Vestir deve recusar. Não troca ped nem força peças inválidas. Perfis diferentes têm bibliotecas independentes.
7. Confira ausência de cobrança e alterações no estoque/caixa PDM, necessidades, veículos/garagem e propriedade. Repita acesso/saída Eclipse, F2, inventário, radial J, F11/teleporte e trabalhos anteriores.

## Configuração e referência

Jobs/Wardrobe.json define Point e Capacity (1–100). Reduzir capacidade não apaga biblioteca existente; bloqueia novos conjuntos além do limite e mantém renomear/substituir/excluir. Configuração inválida usa defaults em memória preservando original. Biblioteca inválida não é sobrescrita pela função de salvar.

Analisados SaveOutfitForCharacter, SaveOutfit, ApplyOutfit_NoSave, DeleteOutfit e RefreshOutfitMenu da PSRP.dll, mais o ponto de guardarroupa em Apartments.OnTick. Código próprio faz snapshot por modelo, operações atômicas, confirmação de exclusão e prévia reversível. A referência captura 12 componentes e 8 props e suporta último conjunto no load; o RPCore preserva cabeça/cabelo e usa 10 componentes de roupa e 5 âncoras usuais, sem autoaplicar no load.

Editor facial/personagem completo, tatuagens/cabelo, loja de roupas, armazenamento interno e hotéis continuam para outras subfases/refinamentos. Não se declara toda moradia concluída. Fases 8/9 mantidas. Build/testes em VALIDACAO.md; comparação em PSRP-SYSTEMS-AUDIT-0.32.0.json. GTA não executado nesta nuvem.
