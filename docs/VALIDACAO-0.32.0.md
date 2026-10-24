# Validação — RPCore 0.32.0

Build Release net48 com SHVDN Enhanced 1.1.0.6/LemonUI 2.2: zero erros/avisos. DLL SHA256 `54237f33ddb6b60f5a9ecc414c819b4118e073da0b4bb9a898ff72fb248ceeda`.

1153 testes aprovados, zero falhas/ignorados: 1125 anteriores e 28 novos. Cobrem ponto de referência, slots/limites/nomes/modelos, save antigo vazio, captura clonada, gravação/substituição/renomeação/exclusão, rollback/replay, limite personalizado, metadados desconhecidos inclusive de peças, disponibilidade/ciclo de variações e isolamento de perfis. Relatórios DataTests.trx/DataTests-0.32.0.trx. Não executam GTA nem validam renderização de roupas.

Campo aditivo Wallet.Wardrobe com Outfits/Receipts, vazio quando ausente. Mudanças clonadas e salvas imediatamente, revertendo o objeto original se falhar. Não cobra, troca ped, altera propriedade, PDM, necessidades ou veículos. Configuração nova Jobs/Wardrobe.json sem regravar Housing.json. Nome duplicado por modelo exige substituição explícita; exclusão tem confirmação; biblioteca mantém itens ao reduzir capacidade.

Guardarroupa do Eclipse no ponto (-797.761,328.302,190.7137), com propriedade/acesso físico obrigatórios. Captura dez componentes de roupa e cinco acessórios, incluindo paleta, sem cabeça 0/cabelo 2. Nome/modelo/índices validados; conjunto de outro modelo ou peça/textura indisponível é recusado antes de aplicar. Aplicação nativa verifica resultado e tenta restaurar captura anterior em falha. Não autoaplica conjunto em reload/perfil, evitando substituição automática de aparência de outros scripts.

Editor de peças/texturas/acessórios com prévia, ciclo incluindo sem acessório, concluir/salvar/cancelar. Backup do mesmo ped/modelo antes de editar. Fechar menu, sair do ponto, abrir interface modal ou trocar perfil cancela prévia e libera atividade; não restaura roupa num ped/modelo diferente. Sem controle persistente congelado, câmera própria ou cabeça/face/tatuagens editadas. Falha de restauração nativa é registrada no log e a atividade é liberada.

Auditoria PSRP-SYSTEMS-AUDIT-0.32.0.json: 75 fontes, baseline 0.31.0, DLL atual; SaveOutfitForCharacter/SaveOutfit/ApplyOutfit_NoSave/DeleteOutfit/RefreshOutfitMenu e ponto Apartments comparados. Referência captura 12 componentes/8 props e suporta auto-load; RPCore usa 10 roupas/5 âncoras usuais, paleta/modelo/validação e edição reversível próprias. Sem editor facial, lojas de roupa, armazenamento interno, hotéis ou toda moradia concluída. Fases 8/9 mantidas.

GTA não executado nesta nuvem. Roteiro FASE-6.2B-0.32.0.md: acesso no andar superior, salvar/vestir/substituir/renomear/excluir, roupas/props/paleta, editor/cancelamento/modelos/perfis/reload e regressão dos sistemas anteriores. Aparência, texturas, peças combinadas e convivência com mods de roupa precisam de teste real.

Instalador executado duas vezes via PowerShell 7/Linux sobre instalação simulada da 0.31.0: 112 arquivos existentes preservados byte a byte, incluindo propriedade Eclipse, saves/configurações/F2 e empresa. Wardrobe.json acrescentado só na primeira execução; personalização conservada na segunda. Backup da DLL anterior e DLL instalada idêntica ao build. Relatório installer-validation-0.32.0.json. Não é teste de gameplay.
