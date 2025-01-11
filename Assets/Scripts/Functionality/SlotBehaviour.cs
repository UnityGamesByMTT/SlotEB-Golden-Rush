using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using System;
using Best.SocketIO;

public class SlotBehaviour : MonoBehaviour
{
    [SerializeField]
    private RectTransform mainContainer_RT;

    [Header("Sprites")]
    [SerializeField]
    private Sprite[] myImages;  //images taken initially

    [Header("Slot Images")]
    [SerializeField]
    private List<SlotImage> images;     //class to store total images
    [SerializeField]
    private List<SlotImage> Tempimages;     //class to store the result matrix

    [Header("Slots Objects")]
    [SerializeField]
    private GameObject[] Slot_Objects;
    [Header("Slots Elements")]
    [SerializeField]
    private LayoutElement[] Slot_Elements;

    [Header("Slots Transforms")]
    [SerializeField]
    private Transform[] Slot_Transform;

    private Dictionary<int, string> y_string = new Dictionary<int, string>();

    [Header("Buttons")]
    [SerializeField]
    private Button SlotStart_Button;
    [SerializeField]
    private Button AutoSpin_Button;
    [SerializeField]
    private Button MaxBet_Button;
    [SerializeField]
    private Button LineBetPlus_Button;
    [SerializeField]
    private Button LineBetMinus_Button;
    [SerializeField]
    private Button AutoSpinStop_Button;
    [SerializeField]
    private Button Turbo_Button;
    [SerializeField]
    private Button StopSpin_Button;



    [Header("Animated Sprites")]
    [SerializeField]
    private Sprite[] PickAxe_Sprite;
    [SerializeField]
    private Sprite[] Lantern_Sprite;
    [SerializeField]
    private Sprite[] Gold_Sprite;
    [SerializeField]
    private Sprite[] MineCart_Sprite;
    [SerializeField]
    private Sprite[] Ten_Sprite;
    [SerializeField]
    private Sprite[] J_Sprite;
    [SerializeField]
    private Sprite[] Q_Sprite;
    [SerializeField]
    private Sprite[] K_Sprite;
    [SerializeField]
    private Sprite[] A_Sprite;
    [SerializeField]
    private Sprite[] Wild_Sprite;
    [SerializeField]
    private Sprite[] Scatter_Sprite;

    [Header("Miscellaneous UI")]
    [SerializeField]
    private TMP_Text Balance_text;
    [SerializeField]
    private TMP_Text TotalBet_text;
    [SerializeField]
    private TMP_Text TotalWin_text;
    [SerializeField]
    private TMP_Text BetPerLine_text;

    protected int Lines = 20;

    [Header("Audio Management")]
    [SerializeField] private AudioController audioController;

    [Header("Free Spins Board")]
    [SerializeField]
    private GameObject FSBoard_Object;
    [SerializeField]
    private TMP_Text FSnum_text;

    [SerializeField]
    private UIManager uiManager;

    int tweenHeight = 0;  //calculate the height at which tweening is done

    [SerializeField]
    private GameObject Image_Prefab;    //icons prefab

    [SerializeField]
    private PayoutCalculation PayCalculator;

    private List<Tweener> alltweens = new List<Tweener>();

    private Tweener WinTween = null;

    [SerializeField]
    private List<ImageAnimation> TempList;  //stores the sprites whose animation is running at present 

    [SerializeField]
    private int IconSizeFactor = 100;       //set this parameter according to the size of the icon and spacing

    private int numberOfSlots = 5;          //number of columns

    [SerializeField]
    int verticalVisibility = 3;

    [SerializeField]
    private SocketIOManager SocketManager;

    Coroutine AutoSpinRoutine = null;
    Coroutine tweenroutine=null;
    private Coroutine FreeSpinRoutine = null;
    internal bool IsAutoSpin = false;
    private bool IsFreeSpin = false;
    bool IsSpinning = false;
    internal bool CheckPopups = false;
    private bool CheckSpinAudio = false;
    bool IsTurboOn;
    private bool StopSpinToggle;
    internal bool WasAutoSpinOn;

    private int BetCounter = 0;
    private double currentBalance = 0;
    private double currentTotalBet = 0;
    private float SpinDelay = 0.2f;

    [SerializeField]
    Sprite[] TurboToggleSprites;

    private void Start()
    {
        IsAutoSpin = false;
        if (SlotStart_Button) SlotStart_Button.onClick.RemoveAllListeners();
        if (SlotStart_Button) SlotStart_Button.onClick.AddListener(delegate { StartSlots(); });

        if (LineBetPlus_Button) LineBetPlus_Button.onClick.RemoveAllListeners();
        if (LineBetPlus_Button) LineBetPlus_Button.onClick.AddListener(delegate { OnBetOne(true); });
        if (LineBetMinus_Button) LineBetMinus_Button.onClick.RemoveAllListeners();
        if (LineBetMinus_Button) LineBetMinus_Button.onClick.AddListener(delegate { OnBetOne(false); });

        if (StopSpin_Button) StopSpin_Button.onClick.RemoveAllListeners();
        if (StopSpin_Button) StopSpin_Button.onClick.AddListener(() => { audioController.PlayButtonAudio(); StopSpinToggle = true; StopSpin_Button.gameObject.SetActive(false); });

        if (Turbo_Button) Turbo_Button.onClick.RemoveAllListeners();
        if (Turbo_Button) Turbo_Button.onClick.AddListener(TurboToggle);

        if (MaxBet_Button) MaxBet_Button.onClick.RemoveAllListeners();
        if (MaxBet_Button) MaxBet_Button.onClick.AddListener(MaxBet);

        if (AutoSpin_Button) AutoSpin_Button.onClick.RemoveAllListeners();
        if (AutoSpin_Button) AutoSpin_Button.onClick.AddListener(AutoSpin);

        if (AutoSpinStop_Button) AutoSpinStop_Button.onClick.RemoveAllListeners();
        if (AutoSpinStop_Button) AutoSpinStop_Button.onClick.AddListener(StopAutoSpin);
        tweenHeight = (16 * IconSizeFactor) - 280;
    }

    void TurboToggle()
    {
        audioController.PlayButtonAudio();
        if (IsTurboOn)
        {
            IsTurboOn = false;
            Turbo_Button.GetComponent<ImageAnimation>().StopAnimation();
            Turbo_Button.image.sprite = TurboToggleSprites[0];
            Turbo_Button.image.color = new Color(0.86f, 0.86f, 0.86f, 1);
        }
        else
        {
            IsTurboOn = true;
            Turbo_Button.GetComponent<ImageAnimation>().StartAnimation();
            Turbo_Button.image.color = new Color(1, 1, 1, 1);
        }
    }

    private void CompareBalance()
    {
        if (currentBalance < currentTotalBet)
        {
            uiManager.LowBalPopup();
        }
    }

    private void AutoSpin()
    {
        if (!IsAutoSpin)
        {

            IsAutoSpin = true;
            if (AutoSpinStop_Button) AutoSpinStop_Button.gameObject.SetActive(true);
            if (AutoSpin_Button) AutoSpin_Button.gameObject.SetActive(false);

            if (AutoSpinRoutine != null)
            {
                StopCoroutine(AutoSpinRoutine);
                AutoSpinRoutine = null;
            }
            AutoSpinRoutine = StartCoroutine(AutoSpinCoroutine());
        
        }
    }

    internal void FreeSpin(int spins)
    {
        if (!IsFreeSpin)
        {
            if (FSnum_text) FSnum_text.text = spins.ToString();
            if (FSBoard_Object) FSBoard_Object.SetActive(true);
            IsFreeSpin = true;
            ToggleButtonGrp(false);

            if (FreeSpinRoutine != null)
            {
                StopCoroutine(FreeSpinRoutine);
                FreeSpinRoutine = null;
            }
            FreeSpinRoutine = StartCoroutine(FreeSpinCoroutine(spins));

        }
    }

    private IEnumerator FreeSpinCoroutine(int spinchances)
    {
        int i = 0;
        while (i < spinchances)
        {
            StartSlots(IsAutoSpin);
            yield return tweenroutine;
            yield return new WaitForSeconds(2);
            i++;
            if (FSnum_text) FSnum_text.text = (spinchances - i).ToString();
        }
        ToggleButtonGrp(true);
        IsFreeSpin = false;
        if (WasAutoSpinOn)
        {
            AutoSpin();
        }
        else
        {
            ToggleButtonGrp(true);
        }
    }

    private void StopAutoSpin()
    {
        
        if (AutoSpinStop_Button.gameObject.activeSelf)
        {
            IsAutoSpin = false;
            if (AutoSpinStop_Button) AutoSpinStop_Button.gameObject.SetActive(false);
            if (AutoSpin_Button) AutoSpin_Button.gameObject.SetActive(true);
            WasAutoSpinOn = false;
            StartCoroutine(StopAutoSpinCoroutine());
        }

    }

    private IEnumerator AutoSpinCoroutine()
    {
        while(IsAutoSpin)
        {
            StartSlots(IsAutoSpin);
            yield return tweenroutine;
        }
    }

    private IEnumerator StopAutoSpinCoroutine()
    {
        yield return new WaitUntil(() => !IsSpinning);
        ToggleButtonGrp(true);
        if (AutoSpinRoutine != null || tweenroutine != null)
        {
            StopCoroutine(AutoSpinRoutine);
            StopCoroutine(tweenroutine);
            tweenroutine = null;
            AutoSpinRoutine = null;
            StopCoroutine(StopAutoSpinCoroutine());
        }
    }

    internal void FetchLines(string LineVal, int count)
    {
        y_string.Add(count + 1, LineVal);
    }

    internal void GenerateStaticLine(TMP_Text LineID_Text)
    {
        DestroyStaticLine();
        int LineID = 1;
        try
        {
            LineID = int.Parse(LineID_Text.text);
        }
        catch (Exception e)
        {
            Debug.Log("Exception while parsing " + e.Message);
        }
        List<int> y_points = null;
        y_points = y_string[LineID]?.Split(',')?.Select(Int32.Parse)?.ToList();
        PayCalculator.GeneratePayoutLinesBackend(y_points, y_points.Count, true);
    }

    //Destroy Static Lines from button hovers
    internal void DestroyStaticLine()
    {
        PayCalculator.ResetStaticLine();
    }

    private void MaxBet()
    {
        if (!WasAutoSpinOn)
        {
            if (audioController) audioController.PlayButtonAudio();
            BetCounter = SocketManager.initialData.Bets.Count - 1;
            if (BetPerLine_text) BetPerLine_text.text = SocketManager.initialData.Bets[BetCounter].ToString();
            if (TotalBet_text) TotalBet_text.text = (SocketManager.initialData.Bets[BetCounter] * Lines).ToString();
            currentTotalBet = SocketManager.initialData.Bets[BetCounter] * Lines;
            CompareBalance();
        }
    }

    void OnBetOne(bool IncDec)
    {
        if (!WasAutoSpinOn)
        {
            if (audioController) audioController.PlayButtonAudio();
            if (IncDec)
            {

                BetCounter++;
                if (BetCounter > SocketManager.initialData.Bets.Count - 1)
                {
                    BetCounter = 0;
                }
            }
            else
            {
                BetCounter--;
                if (BetCounter < 0)
                {
                    BetCounter = SocketManager.initialData.Bets.Count - 1;
                }
            }
            if (BetPerLine_text) BetPerLine_text.text = SocketManager.initialData.Bets[BetCounter].ToString();
            if (TotalBet_text) TotalBet_text.text = (SocketManager.initialData.Bets[BetCounter] * Lines).ToString();
            currentTotalBet = SocketManager.initialData.Bets[BetCounter] * Lines;
            CompareBalance();
        }
    }

    //just for testing purposes delete on production
    //private void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.Space) && SlotStart_Button.interactable)
    //    {
    //        StartSlots();
    //    }
    //}

    internal void shuffleInitialMatrix()
    {
        for (int i = 0; i < Tempimages.Count; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                int randomIndex = UnityEngine.Random.Range(0, myImages.Length);
                Tempimages[i].slotImages[j].sprite = myImages[randomIndex];
            }
        }
    }

    internal void SetInitialUI()
    {
        BetCounter = 0;
        if (BetPerLine_text) BetPerLine_text.text = SocketManager.initialData.Bets[BetCounter].ToString();
        if (TotalBet_text) TotalBet_text.text = (SocketManager.initialData.Bets[BetCounter] * Lines).ToString();
        if (TotalWin_text) TotalWin_text.text = "0.00";
        if (Balance_text) Balance_text.text = SocketManager.playerdata.Balance.ToString("f3");
        currentBalance = SocketManager.playerdata.Balance;
        currentTotalBet = SocketManager.initialData.Bets[BetCounter] * Lines;
        CompareBalance();
        uiManager.InitialiseUIData(SocketManager.initUIData.AbtLogo.link, SocketManager.initUIData.AbtLogo.logoSprite, SocketManager.initUIData.ToULink, SocketManager.initUIData.PopLink, SocketManager.initUIData.paylines);
    }

    //function to populate animation sprites accordingly
    private void PopulateAnimationSprites(ImageAnimation animScript, int val)
    {
        animScript.textureArray.Clear();
        animScript.textureArray.TrimExcess();
        switch (val)
        {
            case 0:
                for (int i = 0; i < PickAxe_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(PickAxe_Sprite[i]);
                }
                break;
            case 1:
                for (int i = 0; i < Lantern_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(Lantern_Sprite[i]);
                }
                break;
            case 2:
                for (int i = 0; i < Gold_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(Gold_Sprite[i]);
                }
                break;
            case 3:
                for (int i = 0; i < MineCart_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(MineCart_Sprite[i]);
                }
                break;
            case 4:
                for (int i = 0; i < Ten_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(Ten_Sprite[i]);
                }
                break;
            case 5:
                for (int i = 0; i < J_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(J_Sprite[i]);
                }
                break;
            case 6:
                for (int i = 0; i < Q_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(Q_Sprite[i]);
                }
                break;
            case 7:
                for (int i = 0; i < K_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(K_Sprite[i]);
                }
                break;
            case 8:
                for (int i = 0; i < A_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(A_Sprite[i]);
                }
                break;
            case 9:
                for (int i = 0; i < Wild_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(Wild_Sprite[i]);
                }
                break;
            case 10:
                for (int i = 0; i < Scatter_Sprite.Length; i++)
                {
                    animScript.textureArray.Add(Scatter_Sprite[i]);
                }
                break;
        }
    }

    //starts the spin process
    private void StartSlots(bool autoSpin = false)
    {
        Debug.Log("called again");
        TotalWin_text.text = "0.00";
        audioController.PlayButtonAudio();
        if (audioController) audioController.PlaySpinButtonAudio();

        if (!autoSpin)
        {
            if (AutoSpinRoutine != null)
            {
                StopCoroutine(AutoSpinRoutine);
                AutoSpinRoutine = null;
            }
        }
        WinningsAnim(false);
        if (SlotStart_Button) SlotStart_Button.interactable = false;
        if (TempList.Count > 0)
        {
            StopGameAnimation();
        }
        PayCalculator.ResetLines();
        tweenroutine=StartCoroutine(TweenRoutine());
        
    }

    private void OnApplicationFocus(bool focus)
    {
        audioController.CheckFocusFunction(focus, CheckSpinAudio);
    }

    private void WinningsAnim(bool IsStart)
    {
        if (IsStart)
        {
            WinTween = TotalWin_text.gameObject.GetComponent<RectTransform>().DOScale(new Vector2(1.5f, 1.5f), 1f).SetLoops(-1, LoopType.Yoyo).SetDelay(0);
        }
        else
        {
            WinTween.Kill();
            TotalWin_text.gameObject.GetComponent<RectTransform>().localScale = Vector3.one;
        }
    }

    private void BalanceDeduction()
    {
        double bet = 0;
        double balance = 0;
        try
        {
            bet = double.Parse(TotalBet_text.text);
        }
        catch (Exception e)
        {
            Debug.Log("Error while conversion " + e.Message);
        }

        try
        {
            balance = double.Parse(Balance_text.text);
        }
        catch (Exception e)
        {
            Debug.Log("Error while conversion " + e.Message);
        }
        double initAmount = balance;

        balance = balance - bet;

        DOTween.To(() => initAmount, (val) => initAmount = val, balance, 0.8f).OnUpdate(() =>
        {
            if (Balance_text) Balance_text.text = initAmount.ToString("f3");
        });
    }

    private IEnumerator TweenRoutine()
    {
        if (currentBalance < currentTotalBet && !IsFreeSpin)
        {
            CompareBalance();
            StopAutoSpin();
            yield return new WaitForSeconds(1);
            ToggleButtonGrp(true);
            yield break;
        }
        if (audioController) audioController.PlayWLAudio("spin");
        
        CheckSpinAudio = true;
        IsSpinning = true;
        ToggleButtonGrp(false);
        if (!IsTurboOn && !IsFreeSpin && !IsAutoSpin)
        {
            StopSpin_Button.gameObject.SetActive(true);
        }
        for (int i = 0; i < numberOfSlots; i++)
        {
            InitializeTweening(Slot_Transform[i]);
            yield return new WaitForSeconds(0.1f);
        }

        if (!IsFreeSpin)
        {
            BalanceDeduction();
        }

        SocketManager.AccumulateResult(BetCounter);

        yield return new WaitUntil(() => SocketManager.isResultdone);

        if (IsAutoSpin)
        {
            WasAutoSpinOn = true;
        }

        for (int j = 0; j < SocketManager.resultData.ResultReel.Count; j++)
        {
            List<int> resultnum = SocketManager.resultData.FinalResultReel[j]?.Split(',')?.Select(Int32.Parse)?.ToList();
            for (int i = 0; i < 5; i++)
            {
                if (images[i].slotImages[images[i].slotImages.Count - 5 + j]) images[i].slotImages[images[i].slotImages.Count - 5 + j].sprite = myImages[resultnum[i]];
                PopulateAnimationSprites(images[i].slotImages[images[i].slotImages.Count - 5 + j].gameObject.GetComponent<ImageAnimation>(), resultnum[i]);
            }
        }
        if (IsTurboOn || IsFreeSpin)
        {

            yield return new WaitForSeconds(0.1f);
        }
        else
        {
            for (int i = 0; i < 5; i++)
            {
                yield return new WaitForSeconds(0.1f);
                if (StopSpinToggle)
                {
                    break;
                }
            }
            StopSpin_Button.gameObject.SetActive(false);
        }


        for (int i = 0; i < numberOfSlots; i++)
        {
            yield return StopTweening(5, Slot_Transform[i], i, StopSpinToggle);
        }
        StopSpinToggle = false;
        yield return alltweens[^1].WaitForCompletion();
        KillAllTweens();
        if (SocketManager.playerdata.currentWining > 0)
        {
            SpinDelay = 1.2f;
        }
        else
        {
            SpinDelay = 0.5f;
        }
        yield return new WaitForSeconds(0.3f);
        CheckPayoutLineBackend(SocketManager.resultData.linesToEmit, SocketManager.resultData.FinalsymbolsToEmit, SocketManager.resultData.jackpot);
      


        CheckPopups = true;

        if (TotalWin_text) TotalWin_text.text = SocketManager.playerdata.currentWining.ToString("f3");

        if (Balance_text) Balance_text.text = SocketManager.playerdata.Balance.ToString("f3");

        currentBalance = SocketManager.playerdata.Balance;

        CheckWinPopups();

        yield return new WaitUntil(() => !CheckPopups);
        if (!IsAutoSpin && !WasAutoSpinOn)
        {
            
            ToggleButtonGrp(true);
            IsSpinning = false;
        }
        else
        {
            
            //yield return new WaitForSeconds(2f);
            IsSpinning = false;
        }
        if (SocketManager.resultData.freeSpins.isNewAdded)
        {
            if (IsFreeSpin)
            {
                IsFreeSpin = false;
                if (FreeSpinRoutine != null)
                {
                    StopCoroutine(FreeSpinRoutine);
                    FreeSpinRoutine = null;
                }
            }
            uiManager.FreeSpinProcess((int)SocketManager.resultData.freeSpins.count);
            if (IsAutoSpin)
            {
                Debug.Log("wasautp");
                StopAutoSpin();
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    internal void CheckWinPopups()
    {
        if (SocketManager.resultData.WinAmout >= currentTotalBet * 10 && SocketManager.resultData.WinAmout < currentTotalBet * 15)
        {
            uiManager.PopulateWin(1, SocketManager.resultData.WinAmout);
        }
        else if (SocketManager.resultData.WinAmout >= currentTotalBet * 15 && SocketManager.resultData.WinAmout < currentTotalBet * 20)
        {
            uiManager.PopulateWin(2, SocketManager.resultData.WinAmout);
        }
        else if (SocketManager.resultData.WinAmout >= currentTotalBet * 20)
        {
            uiManager.PopulateWin(3, SocketManager.resultData.WinAmout);
        }
        else
        {
            CheckPopups = false;
        }
    }

    internal void CallCloseSocket()
    {
        SocketManager.CloseSocket();
    }

    void ToggleButtonGrp(bool toggle)
    {
        
        if (SlotStart_Button) SlotStart_Button.interactable = toggle;
        if (LineBetPlus_Button) LineBetPlus_Button.interactable = toggle;
        if (LineBetMinus_Button) LineBetMinus_Button.interactable = toggle;
        if (MaxBet_Button) MaxBet_Button.interactable = toggle;
        if (AutoSpin_Button) AutoSpin_Button.interactable = toggle;

    }
    //start the icons animation
    private void StartGameAnimation(GameObject animObjects)
    {
        ImageAnimation temp = animObjects.GetComponent<ImageAnimation>();
        temp.StartAnimation();
        TempList.Add(temp);
    }

    //stop the icons animation
    private void StopGameAnimation()
    {
        for (int i = 0; i < TempList.Count; i++)
        {
            TempList[i].StopAnimation();
        }
    }

    private void CheckPayoutLineBackend(List<int> LineId, List<string> points_AnimString, double jackpot = 0)
    {
        
        if (WasAutoSpinOn)
        {
            if (SocketManager.playerdata.currentWining > 0)
            {
                WasAutoSpinOn = true;
                IsAutoSpin = false;
                StopCoroutine(AutoSpinCoroutine());
                Debug.Log("callBoxRoutine");
                if (SocketManager.resultData.WinAmout < currentTotalBet * 10)
                {
                    Invoke("callAutoSpinAgain", 3f);
                }
                
            }
           
        }
    
    List<int> y_points = null;
        List<int> points_anim = null;
        if (LineId.Count > 0 || points_AnimString.Count > 0) 
        {
            if (audioController) audioController.PlayWLAudio("win");

            WinningsAnim(true);

            for (int i = 0; i < LineId.Count; i++)
            {
                y_points = y_string[LineId[i] + 1]?.Split(',')?.Select(Int32.Parse)?.ToList();
                PayCalculator.GeneratePayoutLinesBackend(y_points, y_points.Count);
            }

            if (jackpot > 0)
            {
                for (int i = 0; i < Tempimages.Count; i++)
                {
                    for (int k = 0; k < Tempimages[i].slotImages.Count; k++)
                    {
                        StartGameAnimation(Tempimages[i].slotImages[k].gameObject);
                    }
                }
            }
            else
            {
                for (int i = 0; i < points_AnimString.Count; i++)
                {
                    points_anim = points_AnimString[i]?.Split(',')?.Select(Int32.Parse)?.ToList();

                    for (int k = 0; k < points_anim.Count; k++)
                    {
                        if (points_anim[k] >= 10)
                        {
                            StartGameAnimation(Tempimages[(points_anim[k] / 10) % 10].slotImages[points_anim[k] % 10].gameObject);
                        }
                        else
                        {
                            StartGameAnimation(Tempimages[0].slotImages[points_anim[k]].gameObject);
                        }
                    }
                }
            }
        }
        else
        {
            if (audioController) audioController.StopWLAaudio();
        }
        CheckSpinAudio = false;
    }

    internal void callAutoSpinAgain()
    {
        if (AutoSpinStop_Button.gameObject.activeSelf)
        {
            Debug.Log("callAutoSpinAgain");
            AutoSpin();
        }
    }


    #region TweeningCode
    private void InitializeTweening(Transform slotTransform)
    {
        slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 0);
        Tweener tweener = slotTransform.DOLocalMoveY(-tweenHeight, 0.2f).SetLoops(-1, LoopType.Restart).SetDelay(0);
        tweener.Play();
        alltweens.Add(tweener);
    }



    private IEnumerator StopTweening(int reqpos, Transform slotTransform, int index,bool isStop)
    {
        alltweens[index].Pause();
        int tweenpos = (reqpos * IconSizeFactor) - IconSizeFactor;
        slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, 0);
        alltweens[index] = slotTransform.DOLocalMoveY(-tweenpos + 100, 0.5f).SetEase(Ease.OutElastic);
        if (!isStop)
        {
            yield return new WaitForSeconds(0.2f);
        }
        else
        {
            yield return null;
        }
    }


    private void KillAllTweens()
    {
        for (int i = 0; i < numberOfSlots; i++)
        {
            alltweens[i].Kill();
        }
        alltweens.Clear();

    }
    #endregion

}

[Serializable]
public class SlotImage
{
    public List<Image> slotImages = new List<Image>(10);
}

