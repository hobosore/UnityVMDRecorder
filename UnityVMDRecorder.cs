using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;

public class UnityVMDRecorder : MonoBehaviour
{
    public bool UseParentOfAll = true;
    public bool IgnoreInitialPositionAndRotation = false;
    public bool IsRecording { get; private set; } = false;
    public int FrameNumber { get; private set; } = 0;
    int frameNumberSaved = 0;
    const float FPSs = 0.03333f;
    public enum BoneNames
    {
        全ての親, センター, 左足ＩＫ, 右足ＩＫ, 上半身, 上半身2, 首, 頭,
        左肩, 左腕, 左ひじ, 左手首, 右肩, 右腕, 右ひじ, 右手首,
        左親指１, 左親指２, 左人指１, 左人指２, 左人指３, 左中指１, 左中指２, 左中指３,
        左薬指１, 左薬指２, 左薬指３, 左小指１, 左小指２, 左小指３, 右親指１, 右親指２,
        右人指１, 右人指２, 右人指３, 右中指１, 右中指２, 右中指３, 右薬指１, 右薬指２,
        右薬指３, 右小指１, 右小指２, 右小指３, 左足, 右足, 左ひざ, 右ひざ,
        左足首, 右足首, None
        //左つま先, 右つま先は情報付けると足首の回転、位置との矛盾が生じかねない
    }
    //コンストラクタにて初期化
    public Dictionary<BoneNames, Transform> BoneDictionary { get; private set; }
    Dictionary<BoneNames, Vector3> originalLocalPositionDictionary = new Dictionary<BoneNames, Vector3>();
    Dictionary<BoneNames, Quaternion> originalLocalRotationDictionary = new Dictionary<BoneNames, Quaternion>();
    Dictionary<BoneNames, List<Vector3>> localPositionDictionary = new Dictionary<BoneNames, List<Vector3>>();
    Dictionary<BoneNames, List<Vector3>> localPositionDictionarySaved = new Dictionary<BoneNames, List<Vector3>>();
    Dictionary<BoneNames, List<Quaternion>> localRotationDictionary = new Dictionary<BoneNames, List<Quaternion>>();
    Dictionary<BoneNames, List<Quaternion>> localRotationDictionarySaved = new Dictionary<BoneNames, List<Quaternion>>();
    //ボーン移動量の補正係数
    //この値は大体の値、改良の余地あり
    const float DefaultBoneAmplifier = 16.5f;

    public Vector3 ParentOfAllOffset = new Vector3(0, 0, 0);
    public Vector3 LeftFootIKOffset = Vector3.zero;
    public Vector3 RightFootIKOffset = Vector3.zero;

    private Animator animator;
    BoneGhost boneGhost;

    // Start is called before the first frame update
    void Start()
    {
        Time.fixedDeltaTime = FPSs;
        animator = GetComponent<Animator>();
        BoneDictionary = new Dictionary<BoneNames, Transform>()
            {
                //下半身などというものはUnityにはない
                { BoneNames.全ての親, (animator.transform) },
                { BoneNames.センター, (animator.GetBoneTransform(HumanBodyBones.Hips))},
                { BoneNames.上半身,   (animator.GetBoneTransform(HumanBodyBones.Spine))},
                { BoneNames.上半身2,  (animator.GetBoneTransform(HumanBodyBones.Chest))},
                { BoneNames.頭,       (animator.GetBoneTransform(HumanBodyBones.Head))},
                { BoneNames.首,       (animator.GetBoneTransform(HumanBodyBones.Neck))},
                { BoneNames.左肩,     (animator.GetBoneTransform(HumanBodyBones.LeftShoulder))},
                { BoneNames.右肩,     (animator.GetBoneTransform(HumanBodyBones.RightShoulder))},
                { BoneNames.左腕,     (animator.GetBoneTransform(HumanBodyBones.LeftUpperArm))},
                { BoneNames.右腕,     (animator.GetBoneTransform(HumanBodyBones.RightUpperArm))},
                { BoneNames.左ひじ,   (animator.GetBoneTransform(HumanBodyBones.LeftLowerArm))},
                { BoneNames.右ひじ,   (animator.GetBoneTransform(HumanBodyBones.RightLowerArm))},
                { BoneNames.左手首,   (animator.GetBoneTransform(HumanBodyBones.LeftHand))},
                { BoneNames.右手首,   (animator.GetBoneTransform(HumanBodyBones.RightHand))},
                { BoneNames.左親指１, (animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal))},
                { BoneNames.右親指１, (animator.GetBoneTransform(HumanBodyBones.RightThumbProximal))},
                { BoneNames.左親指２, (animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate))},
                { BoneNames.右親指２, (animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate))},
                { BoneNames.左人指１, (animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal))},
                { BoneNames.右人指１, (animator.GetBoneTransform(HumanBodyBones.RightIndexProximal))},
                { BoneNames.左人指２, (animator.GetBoneTransform(HumanBodyBones.LeftIndexIntermediate))},
                { BoneNames.右人指２, (animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate))},
                { BoneNames.左人指３, (animator.GetBoneTransform(HumanBodyBones.LeftIndexDistal))},
                { BoneNames.右人指３, (animator.GetBoneTransform(HumanBodyBones.RightIndexDistal))},
                { BoneNames.左中指１, (animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal))},
                { BoneNames.右中指１, (animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal))},
                { BoneNames.左中指２, (animator.GetBoneTransform(HumanBodyBones.LeftMiddleIntermediate))},
                { BoneNames.右中指２, (animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate))},
                { BoneNames.左中指３, (animator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal))},
                { BoneNames.右中指３, (animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal))},
                { BoneNames.左薬指１, (animator.GetBoneTransform(HumanBodyBones.LeftRingProximal))},
                { BoneNames.右薬指１, (animator.GetBoneTransform(HumanBodyBones.RightRingProximal))},
                { BoneNames.左薬指２, (animator.GetBoneTransform(HumanBodyBones.LeftRingIntermediate))},
                { BoneNames.右薬指２, (animator.GetBoneTransform(HumanBodyBones.RightRingIntermediate))},
                { BoneNames.左薬指３, (animator.GetBoneTransform(HumanBodyBones.LeftRingDistal))},
                { BoneNames.右薬指３, (animator.GetBoneTransform(HumanBodyBones.RightRingDistal))},
                { BoneNames.左小指１, (animator.GetBoneTransform(HumanBodyBones.LeftLittleProximal))},
                { BoneNames.右小指１, (animator.GetBoneTransform(HumanBodyBones.RightLittleProximal))},
                { BoneNames.左小指２, (animator.GetBoneTransform(HumanBodyBones.LeftLittleIntermediate))},
                { BoneNames.右小指２, (animator.GetBoneTransform(HumanBodyBones.RightLittleIntermediate))},
                { BoneNames.左小指３, (animator.GetBoneTransform(HumanBodyBones.LeftLittleDistal))},
                { BoneNames.右小指３, (animator.GetBoneTransform(HumanBodyBones.RightLittleDistal))},
                { BoneNames.左足ＩＫ, (animator.GetBoneTransform(HumanBodyBones.LeftFoot))},
                { BoneNames.右足ＩＫ, (animator.GetBoneTransform(HumanBodyBones.RightFoot))},
                { BoneNames.左足,     (animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg))},
                { BoneNames.右足,     (animator.GetBoneTransform(HumanBodyBones.RightUpperLeg))},
                { BoneNames.左ひざ,   (animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg))},
                { BoneNames.右ひざ,   (animator.GetBoneTransform(HumanBodyBones.RightLowerLeg))},
                { BoneNames.左足首,   (animator.GetBoneTransform(HumanBodyBones.LeftFoot))},
                { BoneNames.右足首,   (animator.GetBoneTransform(HumanBodyBones.RightFoot))},
                //左つま先, 右つま先は情報付けると足首の回転、位置との矛盾が生じかねない
                //{ BoneNames.左つま先,   (animator.GetBoneTransform(HumanBodyBones.LeftToes))},
                //{ BoneNames.右つま先,   (animator.GetBoneTransform(HumanBodyBones.RightToes))}
        };
        foreach (BoneNames boneName in BoneDictionary.Keys)
        {
            if (BoneDictionary[boneName] == null) { continue; }

            originalLocalPositionDictionary.Add(boneName, BoneDictionary[boneName].localPosition);
            originalLocalRotationDictionary.Add(boneName, BoneDictionary[boneName].localRotation);
        }
        foreach (BoneNames boneName in BoneDictionary.Keys)
        {
            if (BoneDictionary[boneName] == null) { continue; }

            localPositionDictionary.Add(boneName, new List<Vector3>());
            localRotationDictionary.Add(boneName, new List<Quaternion>());
        }

        if (BoneDictionary[BoneNames.左足ＩＫ] != null)
        {
            LeftFootIKOffset = Quaternion.Inverse(transform.rotation) * (BoneDictionary[BoneNames.左足ＩＫ].position - transform.position);
        }

        if (BoneDictionary[BoneNames.右足ＩＫ] != null)
        {
            RightFootIKOffset = Quaternion.Inverse(transform.rotation) * (BoneDictionary[BoneNames.右足ＩＫ].position - transform.position);
        }

        boneGhost = new BoneGhost(animator, BoneDictionary); 
    }

    private void FixedUpdate()
    {
        if (IsRecording)
        {
            SaveFrame();
            FrameNumber++;
        }
    }

    public void SetFPS(int fps)
    {
        Time.fixedDeltaTime = 1 / (float)fps;
    }

    public void StartRecording() { IsRecording = true; }

    public void PauseRecording() { IsRecording = false; }

    public void StopRecording()
    {
        IsRecording = false;
        frameNumberSaved = FrameNumber;
        FrameNumber = 0;
        localPositionDictionarySaved = localPositionDictionary;
        localPositionDictionary = new Dictionary<BoneNames, List<Vector3>>();
        localRotationDictionarySaved = localRotationDictionary;
        localRotationDictionary = new Dictionary<BoneNames, List<Quaternion>>();
    }

    void SaveFrame()
    {
        if (boneGhost != null) { boneGhost.GhostAll(); }

        foreach (BoneNames boneName in BoneDictionary.Keys)
        {
            if (BoneDictionary[boneName] == null)
            {
                continue;
            }

            if (boneName == BoneNames.右足ＩＫ || boneName == BoneNames.左足ＩＫ)
            {
                Vector3 targetVector = BoneDictionary[boneName].position - animator.transform.position;
                targetVector = Quaternion.Inverse(transform.rotation) * targetVector;
                targetVector -= (boneName == BoneNames.左足ＩＫ ? LeftFootIKOffset : RightFootIKOffset);
                Vector3 ikPosition = new Vector3(-targetVector.x, targetVector.y, -targetVector.z);
                localPositionDictionary[boneName].Add(ikPosition * DefaultBoneAmplifier);
                //ikの回転は捨てて回転は全部足首に持たせる
                Quaternion ikRotation = Quaternion.identity;
                localRotationDictionary[boneName].Add(ikRotation);
                continue;
            }

            if (boneGhost != null && boneGhost.GhostDictionary.Keys.Contains(boneName))
            {
                if (boneGhost.GhostDictionary[boneName].ghost == null || !boneGhost.GhostDictionary[boneName].enabled)
                {
                    localRotationDictionary[boneName].Add(Quaternion.identity);
                    localPositionDictionary[boneName].Add(Vector3.zero);
                    continue;
                }

                Vector3 boneVector = boneGhost.GhostDictionary[boneName].ghost.localPosition;
                Quaternion boneQuatenion = boneGhost.GhostDictionary[boneName].ghost.localRotation;
                localRotationDictionary[boneName].Add(new Quaternion(-boneQuatenion.x, boneQuatenion.y, -boneQuatenion.z, boneQuatenion.w));

                boneVector -= boneGhost.OriginalGhostLocalPositionDictionary[boneName];

                localPositionDictionary[boneName].Add(new Vector3(-boneVector.x, boneVector.y, -boneVector.z) * DefaultBoneAmplifier);
                continue;
            }

            Quaternion fixedQuatenion = Quaternion.identity;
            Quaternion vmdRotation = Quaternion.identity;
            if (boneName == BoneNames.全ての親 && !IgnoreInitialPositionAndRotation)
            {
                vmdRotation = new Quaternion(
                    -BoneDictionary[boneName].localRotation.x,
                    BoneDictionary[boneName].localRotation.y,
                    -BoneDictionary[boneName].localRotation.z,
                    BoneDictionary[boneName].localRotation.w);
            }
            else
            {
                fixedQuatenion = BoneDictionary[boneName].localRotation.MinusRotation(originalLocalRotationDictionary[boneName]);
                vmdRotation = new Quaternion(-fixedQuatenion.x, fixedQuatenion.y, -fixedQuatenion.z, fixedQuatenion.w);
            }
            localRotationDictionary[boneName].Add(vmdRotation);

            Vector3 fixedPosition = Vector3.zero;
            Vector3 vmdPosition = Vector3.zero;
            if (boneName == BoneNames.全ての親 && !IgnoreInitialPositionAndRotation)
            {
                vmdPosition = new Vector3(
                -BoneDictionary[boneName].localPosition.x,
                BoneDictionary[boneName].localPosition.y,
                -BoneDictionary[boneName].localPosition.z);
            }
            else
            {
                fixedPosition = new Vector3(
                    BoneDictionary[boneName].localPosition.x - originalLocalPositionDictionary[boneName].x,
                    BoneDictionary[boneName].localPosition.y - originalLocalPositionDictionary[boneName].y,
                    BoneDictionary[boneName].localPosition.z - originalLocalPositionDictionary[boneName].z);

                vmdPosition = new Vector3(-fixedPosition.x, fixedPosition.y, -fixedPosition.z);
            }

            if (boneName == BoneNames.全ての親)
            {
                localPositionDictionary[boneName].Add(vmdPosition * DefaultBoneAmplifier + ParentOfAllOffset);
            }
        }
    }

    public async void SaveVMD(string modelName, string filePath)
    {
        if (IsRecording)
        {
            Debug.Log(transform.name + "VMD保存前にレコーディングをストップしてください。");
            return;
        }

        Debug.Log(transform.name + "VMDファイル作成開始");
        await Task.Run(() =>
        {
            //ファイルの書き込み
            using (BinaryWriter binaryWriter = new BinaryWriter(new FileStream(filePath, FileMode.Create)))
            {
                try
                {
                    const string ShiftJIS = "shift_jis";
                    const int intByteLength = 4;

                    //ファイルタイプの書き込み
                    const int fileTypeLength = 30;
                    const string RightFileType = "Vocaloid Motion Data 0002";
                    byte[] fileTypeBytes = System.Text.Encoding.GetEncoding(ShiftJIS).GetBytes(RightFileType);
                    binaryWriter.Write(fileTypeBytes, 0, fileTypeBytes.Length);
                    binaryWriter.Write(new byte[fileTypeLength - fileTypeBytes.Length], 0, fileTypeLength - fileTypeBytes.Length);

                    //モーション名の書き込み、Shift_JISで保存
                    const int motionNameLength = 20;
                    byte[] motionNameBytes = System.Text.Encoding.GetEncoding(ShiftJIS).GetBytes(modelName);
                    binaryWriter.Write(motionNameBytes, 0, motionNameBytes.Length);
                    binaryWriter.Write(new byte[motionNameLength - motionNameBytes.Length], 0, motionNameLength - motionNameBytes.Length);

                    //全キーフレーム数の書き込み
                    uint allKeyFrameNumber = (uint)frameNumberSaved * (uint)BoneDictionary.Count;
                    byte[] allKeyFrameNumberByte = BitConverter.GetBytes((uint)allKeyFrameNumber);
                    binaryWriter.Write(allKeyFrameNumberByte, 0, intByteLength);

                    //人ボーンの書き込み
                    for (int i = 0; i < frameNumberSaved; i++)
                    {
                        foreach (BoneNames boneName in Enum.GetValues(typeof(BoneNames)))
                        {
                            //if (i != 0
                            //    && localPositionDictionarySaved[boneName][i-1] == localPositionDictionarySaved[boneName][i]
                            //    && localRotationDictionarySaved[boneName][i-1] == localRotationDictionarySaved[boneName][i])
                            //{ continue; }

                            if (!BoneDictionary.Keys.Contains(boneName)) { continue; }
                            if (BoneDictionary[boneName] == null) { continue; }
                            if (!UseParentOfAll && boneName == BoneNames.全ての親) { continue; }

                            const int boneNameLength = 15;
                            string boneNameString = boneName.ToString();
                            byte[] boneNameBytes = System.Text.Encoding.GetEncoding(ShiftJIS).GetBytes(boneNameString);
                            binaryWriter.Write(boneNameBytes, 0, boneNameBytes.Length);
                            binaryWriter.Write(new byte[boneNameLength - boneNameBytes.Length], 0, boneNameLength - boneNameBytes.Length);

                            byte[] frameNumberByte = BitConverter.GetBytes((ulong)i);
                            binaryWriter.Write(frameNumberByte, 0, intByteLength);

                            Vector3 position = localPositionDictionarySaved[boneName][i];
                            byte[] positionX = BitConverter.GetBytes(position.x);
                            binaryWriter.Write(positionX, 0, intByteLength);
                            byte[] positionY = BitConverter.GetBytes(position.y);
                            binaryWriter.Write(positionY, 0, intByteLength);
                            byte[] positionZ = BitConverter.GetBytes(position.z);
                            binaryWriter.Write(positionZ, 0, intByteLength);
                            Quaternion rotation = localRotationDictionarySaved[boneName][i];
                            byte[] rotationX = BitConverter.GetBytes(rotation.x);
                            binaryWriter.Write(rotationX, 0, intByteLength);
                            byte[] rotationY = BitConverter.GetBytes(rotation.y);
                            binaryWriter.Write(rotationY, 0, intByteLength);
                            byte[] rotationZ = BitConverter.GetBytes(rotation.z);
                            binaryWriter.Write(rotationZ, 0, intByteLength);
                            byte[] rotationW = BitConverter.GetBytes(rotation.w);
                            binaryWriter.Write(rotationW, 0, intByteLength);

                            byte[] interpolateBytes = new byte[64];
                            binaryWriter.Write(interpolateBytes, 0, 64);
                        }
                    }

                    //表情モーフの書き込み
                    byte[] faceFrameCount = BitConverter.GetBytes(0);
                    binaryWriter.Write(faceFrameCount, 0, intByteLength);

                    //カメラの書き込み
                    byte[] cameraFrameCount = BitConverter.GetBytes(0);
                    binaryWriter.Write(cameraFrameCount, 0, intByteLength);

                    //照明の書き込み
                    byte[] lightFrameCount = BitConverter.GetBytes(0);
                    binaryWriter.Write(lightFrameCount, 0, intByteLength);

                    //照明の書き込み
                    byte[] selfShadowCount = BitConverter.GetBytes(0);
                    binaryWriter.Write(selfShadowCount, 0, intByteLength);

                    //IKの書き込み
                    //0フレームにキーフレーム一つだけ置く
                    byte[] ikCount = BitConverter.GetBytes(1);
                    byte[] ikFrameNumber = BitConverter.GetBytes(0);
                    byte modelDisplay = Convert.ToByte(1);
                    //右足IKと左足IKと右足つま先IKと左足つま先IKの4つ
                    byte[] ikNumber = BitConverter.GetBytes(4);
                    const int IKNameLength = 20;
                    byte[] leftIKName = System.Text.Encoding.GetEncoding(ShiftJIS).GetBytes("左足ＩＫ");
                    byte[] rightIKName = System.Text.Encoding.GetEncoding(ShiftJIS).GetBytes("右足ＩＫ");
                    byte[] leftToeIKName = System.Text.Encoding.GetEncoding(ShiftJIS).GetBytes("左つま先ＩＫ");
                    byte[] rightToeIKName = System.Text.Encoding.GetEncoding(ShiftJIS).GetBytes("右つま先ＩＫ");
                    byte ikOn = Convert.ToByte(1);
                    byte ikOff = Convert.ToByte(0);
                    binaryWriter.Write(ikCount, 0, intByteLength);
                    binaryWriter.Write(ikFrameNumber, 0, intByteLength);
                    binaryWriter.Write(modelDisplay);
                    binaryWriter.Write(ikNumber, 0, intByteLength);
                    binaryWriter.Write(leftIKName, 0, leftIKName.Length);
                    binaryWriter.Write(new byte[IKNameLength - leftIKName.Length], 0, IKNameLength - leftIKName.Length);
                    binaryWriter.Write(ikOff);
                    binaryWriter.Write(leftToeIKName, 0, leftToeIKName.Length);
                    binaryWriter.Write(new byte[IKNameLength - leftToeIKName.Length], 0, IKNameLength - leftToeIKName.Length);
                    binaryWriter.Write(ikOff);
                    binaryWriter.Write(rightIKName, 0, rightIKName.Length);
                    binaryWriter.Write(new byte[IKNameLength - rightIKName.Length], 0, IKNameLength - rightIKName.Length);
                    binaryWriter.Write(ikOff);
                    binaryWriter.Write(rightToeIKName, 0, rightToeIKName.Length);
                    binaryWriter.Write(new byte[IKNameLength - rightToeIKName.Length], 0, IKNameLength - rightToeIKName.Length);
                    binaryWriter.Write(ikOff);
                }
                catch (Exception ex)
                {
                    Debug.Log("VMD書き込みエラー" + ex.Message);
                }
                finally
                {
                    binaryWriter.Close();
                }
            }
        });
        Debug.Log(transform.name + "VMDファイル作成終了");
    }

    //裏で正規化されたモデル
    //(初期ポーズで各ボーンのlocalRotationがQuaternion.identityのモデル)を疑似的にアニメーションさせる
    class BoneGhost
    {
        public Dictionary<BoneNames, (Transform ghost, bool enabled)> GhostDictionary { get; private set; } = new Dictionary<BoneNames, (Transform ghost, bool enabled)>();
        public Dictionary<BoneNames, Vector3> OriginalGhostLocalPositionDictionary { get; private set; } = new Dictionary<BoneNames, Vector3>();
        public Dictionary<BoneNames, Quaternion> OriginalRotationDictionary { get; private set; } = new Dictionary<BoneNames, Quaternion>();
        public Dictionary<BoneNames, Quaternion> OriginalGhostRotationDictionary { get; private set; } = new Dictionary<BoneNames, Quaternion>();

        private Dictionary<BoneNames, Transform> boneDictionary = new Dictionary<BoneNames, Transform>();

        const string GhostSalt = "Ghost";

        public BoneGhost(Animator animator, Dictionary<BoneNames, Transform> boneDictionary)
        {
            this.boneDictionary = boneDictionary;

            Dictionary<BoneNames, (BoneNames optionParent1, BoneNames optionParent2, BoneNames necessaryParent)> boneParentDictionary
                = new Dictionary<BoneNames, (BoneNames optionParent1, BoneNames optionParent2, BoneNames necessaryParent)>()
            {
                { BoneNames.センター, (BoneNames.None, BoneNames.None, BoneNames.全ての親) },
                { BoneNames.左足,     (BoneNames.None, BoneNames.None, BoneNames.センター) },
                { BoneNames.左ひざ,   (BoneNames.None, BoneNames.None, BoneNames.左足) },
                { BoneNames.左足首,   (BoneNames.None, BoneNames.None, BoneNames.左ひざ) },
                { BoneNames.右足,     (BoneNames.None, BoneNames.None, BoneNames.センター) },
                { BoneNames.右ひざ,   (BoneNames.None, BoneNames.None, BoneNames.右足) },
                { BoneNames.右足首,   (BoneNames.None, BoneNames.None, BoneNames.右ひざ) },
                { BoneNames.上半身,   (BoneNames.None, BoneNames.None, BoneNames.センター) },
                { BoneNames.上半身2,  (BoneNames.None, BoneNames.None, BoneNames.上半身) },
                { BoneNames.首,       (BoneNames.上半身2, BoneNames.None, BoneNames.上半身) },
                { BoneNames.頭,       (BoneNames.首, BoneNames.上半身2, BoneNames.上半身) },
                { BoneNames.左肩,     (BoneNames.上半身2, BoneNames.None, BoneNames.上半身) },
                { BoneNames.左腕,     (BoneNames.左肩, BoneNames.上半身2, BoneNames.上半身) },
                { BoneNames.左ひじ,   (BoneNames.None, BoneNames.None, BoneNames.左腕) },
                { BoneNames.左手首,   (BoneNames.None, BoneNames.None, BoneNames.左ひじ) },
                { BoneNames.左親指１, (BoneNames.左手首, BoneNames.None, BoneNames.None) },
                { BoneNames.左親指２, (BoneNames.左親指１, BoneNames.None, BoneNames.None) },
                { BoneNames.左人指１, (BoneNames.左手首, BoneNames.None, BoneNames.None) },
                { BoneNames.左人指２, (BoneNames.左人指１, BoneNames.None, BoneNames.None) },
                { BoneNames.左人指３, (BoneNames.左人指２, BoneNames.None, BoneNames.None) },
                { BoneNames.左中指１, (BoneNames.左手首, BoneNames.None, BoneNames.None) },
                { BoneNames.左中指２, (BoneNames.左中指１, BoneNames.None, BoneNames.None) },
                { BoneNames.左中指３, (BoneNames.左中指２, BoneNames.None, BoneNames.None) },
                { BoneNames.左薬指１, (BoneNames.左手首, BoneNames.None, BoneNames.None) },
                { BoneNames.左薬指２, (BoneNames.左薬指１, BoneNames.None, BoneNames.None) },
                { BoneNames.左薬指３, (BoneNames.左薬指２, BoneNames.None, BoneNames.None) },
                { BoneNames.左小指１, (BoneNames.左手首, BoneNames.None, BoneNames.None) },
                { BoneNames.左小指２, (BoneNames.左小指１, BoneNames.None, BoneNames.None) },
                { BoneNames.左小指３, (BoneNames.左小指２, BoneNames.None, BoneNames.None) },
                { BoneNames.右肩,     (BoneNames.上半身2, BoneNames.None, BoneNames.上半身) },
                { BoneNames.右腕,     (BoneNames.右肩, BoneNames.上半身2, BoneNames.上半身) },
                { BoneNames.右ひじ,   (BoneNames.None, BoneNames.None, BoneNames.右腕) },
                { BoneNames.右手首,   (BoneNames.None, BoneNames.None, BoneNames.右ひじ) },
                { BoneNames.右親指１, (BoneNames.右手首, BoneNames.None, BoneNames.None) },
                { BoneNames.右親指２, (BoneNames.右親指１, BoneNames.None, BoneNames.None) },
                { BoneNames.右人指１, (BoneNames.右手首, BoneNames.None, BoneNames.None) },
                { BoneNames.右人指２, (BoneNames.右人指１, BoneNames.None, BoneNames.None) },
                { BoneNames.右人指３, (BoneNames.右人指２, BoneNames.None, BoneNames.None) },
                { BoneNames.右中指１, (BoneNames.右手首, BoneNames.None, BoneNames.None) },
                { BoneNames.右中指２, (BoneNames.右中指１, BoneNames.None, BoneNames.None) },
                { BoneNames.右中指３, (BoneNames.右中指２, BoneNames.None, BoneNames.None) },
                { BoneNames.右薬指１, (BoneNames.右手首, BoneNames.None, BoneNames.None) },
                { BoneNames.右薬指２, (BoneNames.右薬指１, BoneNames.None, BoneNames.None) },
                { BoneNames.右薬指３, (BoneNames.右薬指２, BoneNames.None, BoneNames.None) },
                { BoneNames.右小指１, (BoneNames.右手首, BoneNames.None, BoneNames.None) },
                { BoneNames.右小指２, (BoneNames.右小指１, BoneNames.None, BoneNames.None) },
                { BoneNames.右小指３, (BoneNames.右小指２, BoneNames.None, BoneNames.None) },
            };

            //Ghostの生成
            foreach (BoneNames boneName in boneDictionary.Keys)
            {
                if (boneName == BoneNames.全ての親 || boneName == BoneNames.左足ＩＫ || boneName == BoneNames.右足ＩＫ)
                {
                    continue;
                } 

                if (boneDictionary[boneName] == null)
                {
                    GhostDictionary.Add(boneName, (null, false));
                    continue;
                }

                Transform ghost = new GameObject(boneDictionary[boneName].name + GhostSalt).transform;
                ghost.position = boneDictionary[boneName].position;
                ghost.rotation = animator.transform.rotation;
                GhostDictionary.Add(boneName, (ghost, true));
            }

            //Ghostの親子構造を設定
            foreach (BoneNames boneName in boneDictionary.Keys)
            {
                if (boneName == BoneNames.全ての親 || boneName == BoneNames.左足ＩＫ || boneName == BoneNames.右足ＩＫ)
                {
                    continue;
                }

                if (GhostDictionary[boneName].ghost == null || !GhostDictionary[boneName].enabled)
                {
                    continue;
                }

                if (boneName == BoneNames.センター)
                {
                    GhostDictionary[boneName].ghost.SetParent(animator.transform);
                    continue;
                }

                if (boneParentDictionary[boneName].optionParent1 != BoneNames.None && boneDictionary[boneParentDictionary[boneName].optionParent1] != null)
                {
                    GhostDictionary[boneName].ghost.SetParent(GhostDictionary[boneParentDictionary[boneName].optionParent1].ghost);
                }
                else if (boneParentDictionary[boneName].optionParent2 != BoneNames.None && boneDictionary[boneParentDictionary[boneName].optionParent2] != null)
                {
                    GhostDictionary[boneName].ghost.SetParent(GhostDictionary[boneParentDictionary[boneName].optionParent2].ghost);
                }
                else if (boneParentDictionary[boneName].necessaryParent != BoneNames.None && boneDictionary[boneParentDictionary[boneName].necessaryParent] != null)
                {
                    GhostDictionary[boneName].ghost.SetParent(GhostDictionary[boneParentDictionary[boneName].necessaryParent].ghost);
                }
                else
                {
                    GhostDictionary[boneName] = (GhostDictionary[boneName].ghost, false);
                }
            }

            foreach (BoneNames boneName in GhostDictionary.Keys)
            {
                if (GhostDictionary[boneName].ghost == null || !GhostDictionary[boneName].enabled)
                {
                    OriginalGhostLocalPositionDictionary.Add(boneName, Vector3.zero);
                    OriginalGhostRotationDictionary.Add(boneName, Quaternion.identity);
                    OriginalRotationDictionary.Add(boneName, Quaternion.identity);
                }
                else
                {
                    OriginalGhostLocalPositionDictionary.Add(boneName, GhostDictionary[boneName].ghost.localPosition);
                    OriginalGhostRotationDictionary.Add(boneName, GhostDictionary[boneName].ghost.rotation);
                    OriginalRotationDictionary.Add(boneName, boneDictionary[boneName].rotation);
                }
            }
        }

        public void GhostAll()
        {
            foreach (BoneNames boneName in GhostDictionary.Keys)
            {
                if (GhostDictionary[boneName].ghost == null || !GhostDictionary[boneName].enabled) { return; }
                GhostDictionary[boneName].ghost.position = boneDictionary[boneName].position;
                Quaternion transQuaternion = boneDictionary[boneName].rotation * Quaternion.Inverse(OriginalRotationDictionary[boneName]);
                GhostDictionary[boneName].ghost.rotation = transQuaternion * OriginalGhostRotationDictionary[boneName];
            }
        }
    }
}