#pragma strict
var roteSpeed:float = 0.0;

var animationSpeed:float =1.0;
private var animationCount:uint;
private var animationList:Array;
function Start () {
     print("animationGetCount:" + GetComponent.<Animation>().GetClipCount());
     print(GetComponent.<Animation>().clip.name);
     animationCount = GetComponent.<Animation>().GetClipCount();
     print(gameObject.GetComponent.<Animation>());
     animationList = GetAnimationList();
}

function Update () {
  
     transform.eulerAngles.y += roteSpeed;
     GetComponent.<Animation>().CrossFade(animationList[0],0.01);
}

private function GetAnimationList():Array
{
     var tmpArray = new Array();
     for (var state : AnimationState in gameObject.GetComponent.<Animation>())
     {
          tmpArray.Add(state.name);
     }
     return tmpArray;
}