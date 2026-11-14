$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$controllerPath = Join-Path $repoRoot 'ProjetoFinal3D/Assets/Project/Scripts/Enemies/EnemyController.cs'

$stubs = @'
namespace UnityEngine
{
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public sealed class SerializeField : System.Attribute { }
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class RequireComponent : System.Attribute
    {
        public RequireComponent(System.Type first, System.Type second) { }
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z)
        {
            this.x = x; this.y = y; this.z = z;
        }
        public float sqrMagnitude => x * x + y * y + z * z;
        public static Vector3 operator -(Vector3 a, Vector3 b) =>
            new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
    }
    public sealed class Transform
    {
        public Vector3 position;
        public System.Collections.Generic.Dictionary<System.Type, object> Components =
            new System.Collections.Generic.Dictionary<System.Type, object>();
        public T GetComponentInParent<T>() where T : class =>
            Components.TryGetValue(typeof(T), out object value) ? (T)value : null;
    }
    public class MonoBehaviour
    {
        public Transform transform = new Transform();
        public System.Collections.Generic.Dictionary<System.Type, object> Components =
            new System.Collections.Generic.Dictionary<System.Type, object>();
        public T GetComponent<T>() where T : class =>
            Components.TryGetValue(typeof(T), out object value) ? (T)value : null;
    }
    public static class Time { public static float time; }
    public static class Mathf
    {
        public static float Max(float a, float b) => System.Math.Max(a, b);
    }
}
namespace UnityEngine.AI
{
    public enum NavMeshPathStatus { PathComplete, PathPartial, PathInvalid }
    public struct NavMeshHit { }
    public sealed class NavMeshAgent
    {
        public bool isOnNavMesh = true;
        public bool isStopped;
        public bool pathPending;
        public NavMeshPathStatus pathStatus = NavMeshPathStatus.PathComplete;
        public float remainingDistance = 1.2f;
        public bool RaycastBlocked;
        public int DestinationCalls;
        public int ResetCalls;
        public bool SetDestination(UnityEngine.Vector3 target)
        {
            DestinationCalls++;
            return true;
        }
        public void ResetPath() { ResetCalls++; }
        public bool Raycast(UnityEngine.Vector3 target, out NavMeshHit hit)
        {
            hit = new NavMeshHit();
            return RaycastBlocked;
        }
    }
}
public sealed class PlayerHealth
{
    public bool IsDead;
    public int DamageCalls;
    public int DamageTotal;
    public void TakeDamage(int amount) { DamageCalls++; DamageTotal += amount; }
}
public sealed class EnemyHealth { public bool IsDead; }
'@

$source = (Get-Content -LiteralPath $controllerPath -Raw) + [Environment]::NewLine + $stubs
Add-Type -TypeDefinition $source

function New-EnemyFixture {
    $controller = [EnemyController]::new()
    $agent = [UnityEngine.AI.NavMeshAgent]::new()
    $health = [EnemyHealth]::new()
    $controller.Components[[type][UnityEngine.AI.NavMeshAgent]] = $agent
    $controller.Components[[type][EnemyHealth]] = $health
    $awake = [EnemyController].GetMethod('Awake',
        [System.Reflection.BindingFlags]'Instance,NonPublic')
    $null = $awake.Invoke($controller, @())
    return @($controller, $agent, $health)
}

function Invoke-EnemyUpdate($controller) {
    $update = [EnemyController].GetMethod('Update',
        [System.Reflection.BindingFlags]'Instance,NonPublic')
    $null = $update.Invoke($controller, @())
}

$fixture = New-EnemyFixture
$enemy = $fixture[0]
$agent = $fixture[1]
[UnityEngine.Time]::time = 0
Invoke-EnemyUpdate $enemy
if ($agent.DestinationCalls -ne 0 -or -not $agent.isStopped) {
    throw 'Inimigo sem Initialize deve permanecer parado.'
}

$player = [UnityEngine.Transform]::new()
$playerHealth = [PlayerHealth]::new()
$player.Components[[type][PlayerHealth]] = $playerHealth
$enemy.Initialize($player)
Invoke-EnemyUpdate $enemy
if ($agent.DestinationCalls -ne 1 -or $playerHealth.DamageTotal -ne 1) {
    throw 'Inimigo inicializado deve perseguir e causar 1 de dano quando proximo.'
}

[UnityEngine.Time]::time = 0.5
Invoke-EnemyUpdate $enemy
if ($playerHealth.DamageCalls -ne 1) { throw 'Ataque repetiu antes do intervalo.' }

$agent.remainingDistance = 5
[UnityEngine.Time]::time = 1.2
Invoke-EnemyUpdate $enemy
if ($playerHealth.DamageCalls -ne 1) { throw 'Ataque atravessou caminho bloqueado.' }

$agent.remainingDistance = 1.2
$agent.RaycastBlocked = $true
[UnityEngine.Time]::time = 1.3
Invoke-EnemyUpdate $enemy
if ($playerHealth.DamageCalls -ne 1) { throw 'Ataque atravessou parede com caminho antigo.' }

$agent.RaycastBlocked = $false
[UnityEngine.Time]::time = 1.4
Invoke-EnemyUpdate $enemy
if ($playerHealth.DamageCalls -ne 2) { throw 'Ataque nao retomou apos o intervalo.' }

$playerHealth.IsDead = $true
[UnityEngine.Time]::time = 2.5
Invoke-EnemyUpdate $enemy
if (-not $agent.isStopped -or $playerHealth.DamageCalls -ne 2) {
    throw 'Inimigo atacou ou continuou andando apos morte do jogador.'
}

$playerHealth.IsDead = $false
$offMesh = New-EnemyFixture
$offMesh[1].isOnNavMesh = $false
$offMesh[0].Initialize($player)
Invoke-EnemyUpdate $offMesh[0]
if ($offMesh[1].DestinationCalls -ne 0) {
    throw 'Agente fora da NavMesh tentou definir destino.'
}

Write-Output 'PASS EnemyController: alvo, intervalo, caminho e morte do jogador.'
