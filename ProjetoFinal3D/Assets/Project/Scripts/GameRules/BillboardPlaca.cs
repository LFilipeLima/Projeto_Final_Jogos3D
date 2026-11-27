using UnityEngine;

public class BillboardPlaca : MonoBehaviour
{
    [Header("Configurações de Leitura")]
    [Tooltip("Se marcado, a placa gira suavemente para encarar a câmera/jogador sem inclinar para cima ou para baixo.")]
    public bool travarEixoVertical = true;

    [Tooltip("Velocidade de rotação para acompanhar o jogador.")]
    public float suavizacao = 10f;

    [Tooltip("Opcional: arraste o Player aqui se quiser que a placa olhe para o jogador em vez da câmera.")]
    public Transform alvoOpcional;

    private Transform alvo;

    void Start()
    {
        if (alvoOpcional != null)
        {
            alvo = alvoOpcional;
        }
        else if (Camera.main != null)
        {
            alvo = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (alvo == null)
        {
            if (Camera.main != null) alvo = Camera.main.transform;
            else return;
        }

        // Calcula a direção do alvo em relação à placa
        Vector3 direcao = transform.position - alvo.position;

        if (travarEixoVertical)
        {
            direcao.y = 0f;
        }

        if (direcao.sqrMagnitude > 0.001f)
        {
            Quaternion rotacaoAlvo = Quaternion.LookRotation(direcao);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacaoAlvo, Time.deltaTime * suavizacao);
        }
    }
}