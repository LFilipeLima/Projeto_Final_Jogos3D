# Estante de Arquivo 88 — Plano de Implementação

> **Execução aprovada:** trabalhar no checkout atual e produzir somente o asset prefab. O usuário colocará a instância na cena pelo Unity.

**Objetivo:** gerar um prefab decorativo reutilizável de estante para a recepção, sem editar cenas ou objetos da investigação.

**Abordagem:** um comando de Editor cria e salva uma cena temporária aditiva, reabre-a para ativação, monta a hierarquia cúbica, salva `EstanteArquivo.prefab` e fecha/remove a cena temporária. A cena aberta permanece sem alteração. O comando aborta se o prefab já existir, se os materiais necessários não forem encontrados ou se Unity estiver em Play/compilando.

**Tecnologias:** Unity 6000.3.2f1, URP 17.6.0, C#, UnityEditor.

**Referências:** `Mycontext/AGENTS.md`; `Assets/Project/Materials/MAT_Equipamento_Protocolo.mat`; `Assets/Project/Materials/MAT_FITA.mat`.

## Restrições globais

- Usar `ProjetoFinal3D/Assets/Project/Editor/` e `ProjetoFinal3D/Assets/Project/Prefabs/Arena/`.
- Reutilizar os materiais existentes `MAT_Equipamento_Protocolo` e `MAT_FITA`; não criar nem sobrescrever materiais.
- Não editar `Protocolo.unity`, `MesaDeConferencia.prefab` ou scripts de gameplay.
- Preservar as referências e objetos `ArchiveInspection`, `Folha de remessa`, `Caixa 12`, `Caixa 17` e rótulos existentes.
- Não adicionar scripts de runtime, interações, remessas numeradas ou assets de terceiros.
- Não sobrescrever nem duplicar prefab existente.

## Revisão

- O comando interrompe sem deixar objetos na cena aberta quando qualquer precondição falha.
- Partes estruturais têm colliders; caixas e rótulos decorativos não.
- O prefab é orientado com sua frente para `-Z` e seu pivot na base, para facilitar posicionamento na recepção.
- O prefab final deverá aparecer em `Assets/Project/Prefabs/Arena/EstanteArquivo.prefab`; a integração na cena e Play Mode serão feitos pelo usuário.

---

### Tarefa 1: criar o prefab da estante

**Arquivos:**
- Entrega persistida: `ProjetoFinal3D/Assets/Project/Prefabs/Arena/EstanteArquivo.prefab` e `.meta`.
- O gerador de Editor foi usado para produzir/verificar o prefab e removido após a execução, mantendo a entrega focada no asset pedido.

**Interface:** menu `Arquivo 88/Construir Prefab da Estante`; o comando gera um prefab raiz `Estante_Arquivo`, sem criar instância em cena.

- [x] **Passo 1:** implementar o comando com guardas de precondição: não estar em Play Mode nem compilando; prefab de destino ainda inexistente; ambos os materiais existentes e carregáveis.
- [x] **Passo 2:** montar prateleira de aproximadamente `2.4 × 2.0 × 0.45` unidades, com montantes metálicos, três níveis, caixas kraft decorativas e rótulos simples sem números `12` ou `17`.
- [x] **Passo 3:** adicionar colliders somente à estrutura; salvar usando cena temporária aditiva e restaurar cena ativa/estado anterior mesmo se ocorrer exceção; não criar ou alterar a cena `Protocolo`.
- [x] **Passo 4:** compilar/verificar o script, executar `git diff --check` e inspecionar o diff; o comando foi executado no Unity, o prefab está presente, e o usuário poderá posicioná-lo manualmente.

## Resultado e validação

- Prefab gerado e confirmado no Unity: `Assets/Project/Prefabs/Arena/EstanteArquivo.prefab`.
- O gerador temporário passou 14 contratos antes de ser removido.
- Materiais referenciados: `MAT_Equipamento_Protocolo` na estrutura e `MAT_FITA` nas caixas.
- Revisão confirmou root, estrutura, seis caixas, rótulos, colliders apenas na estrutura e nenhum script de gameplay.
- Cena temporária foi removida e a cena ativa retornou para `Protocolo`; `git status` não mostra alterações em cenas.
- Contratos do gerador: 14 passaram. `git diff --check` passou. Unity: sem erros após geração; havia um warning no Console.
- Próximo passo do usuário: arrastar o prefab para a cena e posicionar a estante.
