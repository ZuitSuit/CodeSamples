using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Playables;
public class FloppyReader : MonoBehaviour, AInteractable
{
    public System.Action<MechanismData> OnFloppyInserted;

    [SerializeField] CanvasConsole console;
    [SerializeField] Renderer floppyRenderer;
    [SerializeField] Transform floppySpawnT, floppyMoveToT;
    [SerializeField] Light floppyLight;

    Material floppyMat;
    bool isAvailable;
    Color emissionColor;
    bool isAnimating;

    private void Awake()
    {
        floppyMat = Instantiate(floppyRenderer.material);
        floppyRenderer.material = floppyMat;
        emissionColor = floppyMat.GetColor("_EmissionColor");

        this.enabled = isAvailable;
    }

    public void ToggleAvailable(ConsoleState state, PlayerController player)
    {

        isAvailable = (player != null && state == ConsoleState.PlayerInput && player.InventoryFloppy != null);
        floppyMat.SetColor("_EmissionColor", Color.black);
        floppyLight.enabled = isAvailable;

        this.enabled = isAvailable;
    }

    private void Update()
    {
        Color colorToSet = Color.Lerp(Color.black, emissionColor, (((Mathf.Sin(Time.timeSinceLevelLoad * 10f))+ 1f) / 2f));
        floppyMat.SetColor("_EmissionColor", colorToSet);
    }

    public bool Interact(PlayerController player)
    {
        FloppyDisk disk = player.InventoryFloppy;
        if (disk != null && console.IsInputOn && !isAnimating)
        {
            isAnimating = true;


            disk.ToggleKinematicRB(true);
            //Debug.Log("not null");
            //move it from player cam to drive
            //call computer read floppy method

            //TODO do the floppy animation
            Transform diskT;
            diskT = disk.transform;

            disk.gameObject.SetActive(true);


            diskT.position = floppySpawnT.position;
            diskT.SetParent(floppySpawnT);
            Vector3 originalScale = diskT.localScale;
            diskT.localScale = Vector3.zero;

            diskT.localRotation = Quaternion.identity;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(diskT.DOScale(originalScale, .5f).SetEase(Ease.OutBounce));
            sequence.Append(diskT.DOMove(transform.position, 2f)).SetEase(Ease.InCirc).OnComplete(() => TriggerFloppy(disk));

            return true;
        }

        return false;
    }

    public void TriggerFloppy(FloppyDisk floppy)
    {
        OnFloppyInserted?.Invoke(floppy.Data);
        floppy.SetState(CollectableState.Trash);
        isAnimating = false;
    }

    public bool NeedsFocus() => false;
}
