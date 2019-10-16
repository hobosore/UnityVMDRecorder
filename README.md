# UnityVMDRecorder

Unity上の人・カメラアニメーションをランタイムで記録してvmd(MMDのアニメーションファイル)に保存するためのコードです。  
This code enables you to record humanoid/camera animations in unity into .vmd file (MikuMikuDance).  
  
モーフも記録できるようにしました。 (2019/08/05)  
Added function to record morph animations.

MMD上ではセンターボーンが足元にあるモデルに対して補正できるように (2019/08/06)  
Supported models whose center bone in MMD is at its foot.  
  
全ての親をセンターに移すオプションを追加  
MMD上で全ての親を編集できるように(2019/10/16)
Added UseCenterAsParentOfAll(public bool).
  
  
**MIT License**
