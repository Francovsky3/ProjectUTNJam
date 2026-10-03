using UnityEngine;
using UnityEngine.UI;

public class SliderVolumen : MonoBehaviour
{
    public Slider slider;
    public Button botonIcono;
    public Sprite iconoVolumen, iconoMute;

    float volumenAntesDeMute = 0.8f;
    Image icono;

    void Start()
    {
        icono = botonIcono.GetComponent<Image>();

        slider.value = PlayerPrefs.GetFloat("volumen", 0.8f);
        slider.onValueChanged.AddListener(Cambiar);
        botonIcono.onClick.AddListener(AlternarMute);

        Cambiar(slider.value);
    }

    void Cambiar(float v)
    {
        AudioListener.volume = v;
        icono.sprite = v <= 0.001f ? iconoMute : iconoVolumen;
        PlayerPrefs.SetFloat("volumen", v);
    }

    void AlternarMute()
    {
        if (slider.value > 0.001f)
        {
            volumenAntesDeMute = slider.value;   // guarda dónde estaba
            slider.value = 0f;                   // silencia
        }
        else
        {
            slider.value = volumenAntesDeMute;   // vuelve al volumen anterior
        }
        // no hace falta llamar a Cambiar: el slider dispara onValueChanged solo
    }
}