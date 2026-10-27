# Validação0.91.0 — sono/desmaio e veículos

3449 testes aprovados (3415 anteriores +34 novos); Release net48 sem erros/avisos. DataTests-0.91.0.trx. Nenhuma execução GTA/PSRP.dll neste ambiente.

Cobertura: recuperação única10s/sono+stamina100/cooldown0 sem ganho de comida/água/vida, save-before-effect e rollback em início/final, estado concluído não reinicia episódio após falha, tempo restante/recarga/metadados, pausa/stalls,14condições de exclusão incluindo veículo/embarque, cama com contexto/salto4–12h/madrugada e exclusão de relógio manual/viagem/offline; refeição pendente não desfaz descanso, formato inválido preservado.

Inspeção/compilação: uma chamada boundedRagdoll por episódio; sem loop30s; cancelamento por perfil/veículo/missão, gate liberado apenas pelo dono, input RPCore suspenso, fadiga sem Shift bloqueado em carro, nenhum clear global de tarefas/warp/ejeção/alteração de veículo. Sono da cama usa heurística contextual de safehouses/fade/controle/relógio, não evento oficial. CORRECAO-SONO-0.91.0.md contém roteiro GTA e limites. Não declara animação/cama homologadas.

Instalador PowerShell7/Linux: duas atualizacoes sobre0.90.0 em GTA simulado, 187 arquivos existentes preservados byte por byte. Casos focados: Needs legado/inicio/parcial/concluido/invalido/recuperado, backups, recibos/comida/inventario/slots/containers, saldos e preferencias de mundo; DLL anterior em backup e arquivo de outro mod intocado. Relatorio installer-validation-0.91.0.json.
