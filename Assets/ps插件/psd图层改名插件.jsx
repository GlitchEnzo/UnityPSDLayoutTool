#target photoshopapp.bringToFront(); 

if (documents.length == 0) 
{ 
    alert("没有可处理的文档"); 
} 
else
{ 

    var visibility = false; 
    var docRef = activeDocument; 
    var layers = docRef.layers; 

    if (layers.length == 1 && docRef.activeLayer.isBackgroundLayer == 1)
    { 
        alert("The Background layer can not be hidden when it is the only layer in a document."); 
    }     
    else
    { 
        for (var i = 0; i < layers.length; i++)
	    { 
            layers[i].name = "mytestname_"+i; //这里改成自己要的名字
        } 
    } 
}