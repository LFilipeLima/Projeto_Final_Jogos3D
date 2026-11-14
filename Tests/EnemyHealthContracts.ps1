$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$damageablePath = Join-Path $repoRoot 'ProjetoFinal3D/Assets/Project/Scripts/Shared/IDamageable.cs'
$enemyHealthPath = Join-Path $repoRoot 'ProjetoFinal3D/Assets/Project/Scripts/Enemies/EnemyHealth.cs'

$stubs = @'
namespace UnityEngine
{
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public sealed class SerializeField : System.Attribute { }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z)
        {
            this.x = x; this.y = y; this.z = z;
        }
    }

    public sealed class Transform
    {
        public Vector3 position;
    }

    public sealed class GameObject { }

    public class MonoBehaviour
    {
        public static System.Action<GameObject> DestroyObserver;
        public Transform transform = new Transform();
        public GameObject gameObject = new GameObject();
        protected static void Destroy(GameObject target)
        {
            DestroyObserver?.Invoke(target);
        }
    }

    public static class Mathf
    {
        public static int Max(int a, int b) => System.Math.Max(a, b);
    }
}
'@

$source = (Get-Content -LiteralPath $enemyHealthPath -Raw) + [Environment]::NewLine +
    (Get-Content -LiteralPath $damageablePath -Raw) + [Environment]::NewLine +
    $stubs
Add-Type -TypeDefinition $source

$enemy = [EnemyHealth]::new()
$enemy.transform.position = [UnityEngine.Vector3]::new(3, 0, -2)
$awake = [EnemyHealth].GetMethod('Awake',
    [System.Reflection.BindingFlags]'Instance,NonPublic')
$null = $awake.Invoke($enemy, @())

$script:events = 0
$script:destroyed = 0
$script:sequence = [System.Collections.Generic.List[string]]::new()
$script:reportedPosition = [UnityEngine.Vector3]::new(0, 0, 0)

$handler = [System.Action[UnityEngine.Vector3]] {
    param($position)
    $script:events++
    $script:reportedPosition = $position
    $script:sequence.Add('Died')
}
$enemy.add_Died($handler)
[UnityEngine.MonoBehaviour]::DestroyObserver = [System.Action[UnityEngine.GameObject]] {
    param($target)
    $script:destroyed++
    $script:sequence.Add('Destroy')
}

if ($enemy.CurrentHealth -ne 2) { throw 'Vida inicial deve ser 2.' }
$enemy.TakeDamage(0)
if ($enemy.CurrentHealth -ne 2) { throw 'Dano zero alterou a vida.' }
$enemy.TakeDamage(1)
if ($enemy.CurrentHealth -ne 1 -or $script:events -ne 0) {
    throw 'Primeiro tiro deve deixar 1 de vida sem evento.'
}
$enemy.TakeDamage(1)
$enemy.TakeDamage(1)

if ($enemy.CurrentHealth -ne 0) { throw 'Segundo tiro deve zerar a vida.' }
if ($script:events -ne 1) { throw "Died ocorreu $script:events vezes." }
if ($script:destroyed -ne 1) { throw "Destroy ocorreu $script:destroyed vezes." }
if ($script:reportedPosition.x -ne 3 -or $script:reportedPosition.z -ne -2) {
    throw 'Died recebeu posicao incorreta.'
}
if (($script:sequence -join ',') -ne 'Died,Destroy') {
    throw "Ordem incorreta: $($script:sequence -join ',')."
}

Write-Output 'PASS EnemyHealth: 2 acertos, evento unico e posicao antes de Destroy.'
