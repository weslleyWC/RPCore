# RPCore 0.34.0 — fase 6.3A: barbearias / cortes e olhos

## Instalação

Extraia RPCore-update-0.34.0.zip e abra PowerShell na pasta com Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Ajuste para seu caminho. Requer GTA V Legacy, SHVDN Enhanced 1.1.0.6 e LemonUI 2.2. Não execute PSRP.dll como segundo script. Sem reinstalar .oiv/.rpf. Acrescenta Jobs/Barber.json e conserva saves/configurações anteriores, imóvel/roupas/itens/PDM/F2/F11/F8.

## Como usar

F10 → Barbearias / cortes e olhos → GPS da loja. Sete pontos da PSRP: Davis, Rockford, Vespucci, Sandy Shores, Mirror Park, Hawick e Paleto, com ícones de barbearia no mapa. A pé a até 2,5m do ponto, fora de missão/procurado/combate e sem atividade/outra câmera ativa, selecione Iniciar previa pelo F10. A tecla E continua pertencendo ao atendimento nativo do GTA, sem substituição pelo RPCore.

Use câmera normal em terceira pessoa antes de iniciar para conferir o cabelo. Corte anterior/Proximo corte navegam pelas variações reais do ped atual. Textura anterior/Proxima textura alteram texturas do corte. Girar esquerda/direita orienta o personagem para conferir o visual; a orientação original volta ao sair. Protagonistas usam apenas as variações que seu modelo disponibiliza. Nos freemode com cor de olhos capturável, aparecem também as opções de olhos (0–31). Não troca ped nem cria um freemode automaticamente.

A prévia é reversível. Cancelar, fechar menu, abrir F2/F11/outra interface, afastar-se, morrer, iniciar missão, ganhar procurado ou trocar perfil/modelo interrompe sem cobrar. Restaura somente campos capturados do mesmo ped/modelo, sem aplicar backup a outro personagem. Usa câmera normal; não teleporta, imobiliza persistentemente ou remove o barbeiro do ambiente.

Confirmar e pagar cobra **$150 do banco RPCore**, apenas se houve mudança. Não cobra prévia nem confirmação sem alteração. Se faltar saldo, mantém a possibilidade de cancelar sem pagar. O último visual confirmado do modelo fica salvo por perfil com contador/despesa; ao recarregar, não cobra nem autoaplica aparência.

Reaplicar ultimo visual confirmado funciona na barbearia e no mesmo modelo, sem nova cobrança. É uma restauração manual do visual já adquirido, não contratação de outro serviço. Recursos/variações incompatíveis são recusados. Para novo corte, entre na prévia e confirme normalmente.

## Escopo da subfase

Cabelo: componente 2, textura e paleta. Olhos: apenas freemode com getter válido. Não edita tintura/mechas, barba, sobrancelhas, rosto genético, maquiagem, tatuagens ou roupas. Tintas atuais não são lidas de funções erradas nem substituídas por valores de personagem novo. Esses grupos seguem em próximas subfases/refinamentos; não se declara criador de personagem completo.

A roupa salva no guardarroupa continua sem cabeça/cabelo e não sobrescreve os cortes. Aparência de outros scripts não é substituída automaticamente ao carregar. Mudanças nos recursos de cabelo podem alterar índices, exigindo novo atendimento/regravação do visual.

## Roteiro no GTA

1. Atualize e confira F10, sete ícones e GPS. Sem atendimento, cabelo/olhos/banco não devem mudar. Vá a uma loja pelo GPS, fique a pé perto do ponto e inicie pelo F10. Não use E para abrir o RPCore; E continua vanilla.
2. Confira corte/textura/giro em protagonista com variações ou freemode. Cancele: cabelo/textura/paleta e orientação devem voltar, sem débito. Em freemode, altere olhos e cancele para conferir restauração. Protagonista não deve receber opção de olhos.
3. Faça uma mudança e confirme: débito único de $150 do banco RPCore, não do caixa PDM nem dinheiro em mãos. Sem mudança não cobra. Sem saldo deve recusar confirmação, mantendo cancelamento. Price=0 pode ser configurado para teste gratuito, com jogo fechado.
4. Reinicie scripts/jogo: último visual e contadores ficam salvos, sem cobrança/aplicação automática. Vá à loja e use Reaplicar para conferir restauração manual sem novo débito. Faça outro serviço para atualizar o registro do mesmo modelo, sem duplicar registros.
5. Durante prévia, feche menu/abra F2/F11/afaste-se. Deve restaurar e liberar a atividade. Teste missão/procurado/perfil/modelo conforme seu cenário, conferindo que não aplica corte do ped antigo no novo e não altera estrelas, roupa, barba, tintura ou rosto.
6. Confira atendimento nativo separadamente usando E com o menu RPCore fechado. Não deve apagar NPCs, trocar controles persistentes ou interromper câmera vanilla. Caso outra câmera esteja ativa, RPCore deve recusar a prévia.
7. Repita apartamento/entrada/saída, guardarroupa, armazenamento privado, F8/trunk, inventário, radial J, F2, F11/teleporte, trabalhos e sistemas PDM. Nenhuma compra de imóvel, item ou roupa deve ser reiniciada.

## Configuração e referência

Jobs/Barber.json contém Price (0–10000) e Shops. Preço de $150 é decisão própria do RPCore. Os sete pontos vêm de BarberMarkers na PSRP/MultiplayerCharacter.cs. Configuração inválida usa defaults em memória, mantendo o arquivo original. O preço exibido no atendimento é o da configuração carregada na sessão.

Analisados BuildBarberMenu, ApplyHair, ApplyFacialHair e opções da referência. A PSRP tem tintas/barba/sobrancelhas e câmera dedicada; esta subfase trata apenas campos capturáveis com segurança, prévia/compra próprias e acesso F10. GET_PED_HAIR_TINT_COLOR (0x4852FC386E2E1BB5) é consulta RGB de paleta, não cor atual do ped; GET_PED_HEAD_OVERLAY retorna apenas índice. Não foram usados offsets de memória ou assinaturas adivinhadas para montar backup. Conferência adicional: https://github.com/alloc8or/gta5-nativedb-data/blob/master/natives.json .

Editor facial completo, cores/barba e demais diferenças continuam pendentes. Fases 8 (interface/mecânicas) e 9 (refinamentos dos trabalhos/serviços) mantidas. Veja VALIDACAO.md e PSRP-SYSTEMS-AUDIT-0.34.0.json. GTA não foi executado nesta nuvem.
