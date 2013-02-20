mw.loader.implement("jquery.client",function($){(function($){var profileCache={};$.client={profile:function(nav){if(nav===undefined){nav=window.navigator;}if(profileCache[nav.userAgent]===undefined){var uk='unknown';var x='x';var wildUserAgents=['Opera','Navigator','Minefield','KHTML','Chrome','PLAYSTATION 3'];var userAgentTranslations=[[/(Firefox|MSIE|KHTML,\slike\sGecko|Konqueror)/,''],['Chrome Safari','Chrome'],['KHTML','Konqueror'],['Minefield','Firefox'],['Navigator','Netscape'],['PLAYSTATION 3','PS3']];var versionPrefixes=['camino','chrome','firefox','netscape','netscape6','opera','version','konqueror','lynx','msie','safari','ps3'];var versionSuffix='(\\/|\\;?\\s|)([a-z0-9\\.\\+]*?)(\\;|dev|rel|\\)|\\s|$)';var names=['camino','chrome','firefox','netscape','konqueror','lynx','msie','opera','safari','ipod','iphone','blackberry','ps3'];var nameTranslations=[];var layouts=['gecko','konqueror','msie','opera','webkit'];var layoutTranslations=[['konqueror','khtml'],['msie','trident'],[
'opera','presto']];var layoutVersions=['applewebkit','gecko'];var platforms=['win','mac','linux','sunos','solaris','iphone'];var platformTranslations=[['sunos','solaris']];var translate=function(source,translations){for(var i=0;i<translations.length;i++){source=source.replace(translations[i][0],translations[i][1]);}return source;};var ua=nav.userAgent,match,name=uk,layout=uk,layoutversion=uk,platform=uk,version=x;if(match=new RegExp('('+wildUserAgents.join('|')+')').exec(ua)){ua=translate(ua,userAgentTranslations);}ua=ua.toLowerCase();if(match=new RegExp('('+names.join('|')+')').exec(ua)){name=translate(match[1],nameTranslations);}if(match=new RegExp('('+layouts.join('|')+')').exec(ua)){layout=translate(match[1],layoutTranslations);}if(match=new RegExp('('+layoutVersions.join('|')+')\\\/(\\d+)').exec(ua)){layoutversion=parseInt(match[2],10);}if(match=new RegExp('('+platforms.join('|')+')').exec(nav.platform.toLowerCase())){platform=translate(match[1],platformTranslations);}if(match=new
RegExp('('+versionPrefixes.join('|')+')'+versionSuffix).exec(ua)){version=match[3];}if(name.match(/safari/)&&version>400){version='2.0';}if(name==='opera'&&version>=9.8){version=ua.match(/version\/([0-9\.]*)/i)[1]||10;}var versionNumber=parseFloat(version,10)||0.0;profileCache[nav.userAgent]={'name':name,'layout':layout,'layoutVersion':layoutversion,'platform':platform,'version':version,'versionBase':(version!==x?Math.floor(versionNumber).toString():x),'versionNumber':versionNumber};}return profileCache[nav.userAgent];},test:function(map,profile){profile=$.isPlainObject(profile)?profile:$.client.profile();var dir=$('body').is('.rtl')?'rtl':'ltr';if(typeof map[dir]!=='object'||typeof map[dir][profile.name]==='undefined'){return true;}var conditions=map[dir][profile.name];for(var i=0;i<conditions.length;i++){var op=conditions[i][0];var val=conditions[i][1];if(val===false){return false;}else if(typeof val=='string'){if(!(eval('profile.version'+op+'"'+val+'"'))){return false;}}else if(
typeof val=='number'){if(!(eval('profile.versionNumber'+op+val))){return false;}}}return true;}};})(jQuery);;},{},{});mw.loader.implement("jquery.cookie",function($){jQuery.cookie=function(name,value,options){if(typeof value!='undefined'){options=options||{};if(value===null){value='';options.expires=-1;}var expires='';if(options.expires&&(typeof options.expires=='number'||options.expires.toUTCString)){var date;if(typeof options.expires=='number'){date=new Date();date.setTime(date.getTime()+(options.expires*24*60*60*1000));}else{date=options.expires;}expires='; expires='+date.toUTCString();}var path=options.path?'; path='+(options.path):'';var domain=options.domain?'; domain='+(options.domain):'';var secure=options.secure?'; secure':'';document.cookie=[name,'=',encodeURIComponent(value),expires,path,domain,secure].join('');}else{var cookieValue=null;if(document.cookie&&document.cookie!=''){var cookies=document.cookie.split(';');for(var i=0;i<cookies.length;i++){var cookie=jQuery.trim(
cookies[i]);if(cookie.substring(0,name.length+1)==(name+'=')){cookieValue=decodeURIComponent(cookie.substring(name.length+1));break;}}}return cookieValue;}};;},{},{});mw.loader.implement("jquery.messageBox",function($){(function($){$.messageBoxNew=function(options){options=$.extend({'id':'js-messagebox','parent':'body','insert':'prepend'},options);var $curBox=$('#'+options.id);if($curBox.length>0){if($curBox.hasClass('js-messagebox')){return $curBox;}else{return $curBox.addClass('js-messagebox');}}else{var $newBox=$('<div>',{'id':options.id,'class':'js-messagebox','css':{'display':'none'}});if($(options.parent).length<1){options.parent='body';}if(options.insert==='append'){$newBox.appendTo(options.parent);return $newBox;}else{$newBox.prependTo(options.parent);return $newBox;}}};$.messageBox=function(options){options=$.extend({'message':'','group':'default','replace':false,'target':'js-messagebox'},options);var $target=$.messageBoxNew({id:options.target});var groupID=options.target+'-'+
options.group;var $group=$('#'+groupID);if($group.length<1){$group=$('<div>',{'id':groupID,'class':'js-messagebox-group'});$target.prepend($group);}if(options.replace===true){$group.empty();}if(options.message===''||options.message===null){$group.hide();}else{$group.prepend($('<p>').append(options.message)).show();$target.slideDown();}if($target.find('> *:visible').length===0){$group.show();$target.slideUp();$group.hide();}else{$target.slideDown();}return $group;};})(jQuery);;},{"all":".js-messagebox{margin:1em 5%;padding:0.5em 2.5%;border:1px solid #ccc;background-color:#fcfcfc;font-size:0.8em}.js-messagebox .js-messagebox-group{margin:1px;padding:0.5em 2.5%;border-bottom:1px solid #ddd}.js-messagebox .js-messagebox-group:last-child{border-bottom:thin none transparent}\n\n/* cache key: wikidb-mw_:resourceloader:filter:minify-css:7:8b08bdc91c52a9ffba396dccfb5b473c */\n"},{});mw.loader.implement("jquery.mwExtension",function($){(function($){$.extend({trimLeft:function(str){return str===
null?'':str.toString().replace(/^\s+/,'');},trimRight:function(str){return str===null?'':str.toString().replace(/\s+$/,'');},ucFirst:function(str){return str.charAt(0).toUpperCase()+str.substr(1);},escapeRE:function(str){return str.replace(/([\\{}()|.?*+\-^$\[\]])/g,"\\$1");},isDomElement:function(el){return!!el&&!!el.nodeType;},isEmpty:function(v){if(v===''||v===0||v==='0'||v===null||v===false||v===undefined){return true;}if(v.length===0){return true;}if(typeof v==='object'){for(var key in v){return false;}return true;}return false;},compareArray:function(arrThis,arrAgainst){if(arrThis.length!=arrAgainst.length){return false;}for(var i=0;i<arrThis.length;i++){if($.isArray(arrThis[i])){if(!$.compareArray(arrThis[i],arrAgainst[i])){return false;}}else if(arrThis[i]!==arrAgainst[i]){return false;}}return true;},compareObject:function(objectA,objectB){if(typeof objectA==typeof objectB){if(typeof objectA=='object'){if(objectA===objectB){return true;}else{var prop;for(prop in objectA){if(
prop in objectB){var type=typeof objectA[prop];if(type==typeof objectB[prop]){switch(type){case'object':if(!$.compareObject(objectA[prop],objectB[prop])){return false;}break;case'function':if(objectA[prop].toString()!==objectB[prop].toString()){return false;}break;default:if(objectA[prop]!==objectB[prop]){return false;}break;}}else{return false;}}else{return false;}}for(prop in objectB){if(!(prop in objectA)){return false;}}}}}else{return false;}return true;}});})(jQuery);;},{},{});mw.loader.implement("mediawiki.legacy.ajax",function($){window.sajax_debug_mode=false;window.sajax_request_type='GET';window.sajax_debug=function(text){if(!sajax_debug_mode)return false;var e=document.getElementById('sajax_debug');if(!e){e=document.createElement('p');e.className='sajax_debug';e.id='sajax_debug';var b=document.getElementsByTagName('body')[0];if(b.firstChild){b.insertBefore(e,b.firstChild);}else{b.appendChild(e);}}var m=document.createElement('div');m.appendChild(document.createTextNode(text))
;e.appendChild(m);return true;};window.sajax_init_object=function(){sajax_debug('sajax_init_object() called..');var A;try{A=new XMLHttpRequest();}catch(e){try{A=new ActiveXObject('Msxml2.XMLHTTP');}catch(e){try{A=new ActiveXObject('Microsoft.XMLHTTP');}catch(oc){A=null;}}}if(!A){sajax_debug('Could not create connection object.');}return A;};window.sajax_do_call=function(func_name,args,target){var i,x,n;var uri;var post_data;uri=mw.util.wikiScript()+'?action=ajax';if(sajax_request_type=='GET'){if(uri.indexOf('?')==-1){uri=uri+'?rs='+encodeURIComponent(func_name);}else{uri=uri+'&rs='+encodeURIComponent(func_name);}for(i=0;i<args.length;i++){uri=uri+'&rsargs[]='+encodeURIComponent(args[i]);}post_data=null;}else{post_data='rs='+encodeURIComponent(func_name);for(i=0;i<args.length;i++){post_data=post_data+'&rsargs[]='+encodeURIComponent(args[i]);}}x=sajax_init_object();if(!x){alert('AJAX not supported');return false;}try{x.open(sajax_request_type,uri,true);}catch(e){if(window.location.
hostname=='localhost'){alert("Your browser blocks XMLHttpRequest to 'localhost', try using a real hostname for development/testing.");}throw e;}if(sajax_request_type=='POST'){x.setRequestHeader('Method','POST '+uri+' HTTP/1.1');x.setRequestHeader('Content-Type','application/x-www-form-urlencoded');}x.setRequestHeader('Pragma','cache=yes');x.setRequestHeader('Cache-Control','no-transform');x.onreadystatechange=function(){if(x.readyState!=4){return;}sajax_debug('received ('+x.status+' '+x.statusText+') '+x.responseText);if(typeof(target)=='function'){target(x);}else if(typeof(target)=='object'){if(target.tagName=='INPUT'){if(x.status==200){target.value=x.responseText;}}else{if(x.status==200){target.innerHTML=x.responseText;}else{target.innerHTML='<div class="error">Error: '+x.status+' '+x.statusText+' ('+x.responseText+')</div>';}}}else{alert('bad target for sajax_do_call: not a function or object: '+target);}};sajax_debug(func_name+' uri = '+uri+' / post = '+post_data);x.send(post_data)
;sajax_debug(func_name+' waiting..');delete x;return true;};window.wfSupportsAjax=function(){var request=sajax_init_object();var supportsAjax=request?true:false;delete request;return supportsAjax;};;},{},{});mw.loader.implement("mediawiki.legacy.wikibits",function($){(function(){window.clientPC=navigator.userAgent.toLowerCase();window.is_gecko=/gecko/.test(clientPC)&&!/khtml|spoofer|netscape\/7\.0/.test(clientPC);window.is_safari=window.is_safari_win=window.webkit_version=window.is_chrome=window.is_chrome_mac=false;window.webkit_match=clientPC.match(/applewebkit\/(\d+)/);if(webkit_match){window.is_safari=clientPC.indexOf('applewebkit')!=-1&&clientPC.indexOf('spoofer')==-1;window.is_safari_win=is_safari&&clientPC.indexOf('windows')!=-1;window.webkit_version=parseInt(webkit_match[1]);window.is_chrome=clientPC.indexOf('chrome')!==-1&&clientPC.indexOf('spoofer')===-1;window.is_chrome_mac=is_chrome&&clientPC.indexOf('mac')!==-1}window.is_ff2=/firefox\/[2-9]|minefield\/3/.test(clientPC);
window.ff2_bugs=/firefox\/2/.test(clientPC);window.is_ff2_win=is_ff2&&clientPC.indexOf('windows')!=-1;window.is_ff2_x11=is_ff2&&clientPC.indexOf('x11')!=-1;window.is_opera=window.is_opera_preseven=window.is_opera_95=window.opera6_bugs=window.opera7_bugs=window.opera95_bugs=false;if(clientPC.indexOf('opera')!=-1){window.is_opera=true;window.is_opera_preseven=window.opera&&!document.childNodes;window.is_opera_seven=window.opera&&document.childNodes;window.is_opera_95=/opera\/(9\.[5-9]|[1-9][0-9])/.test(clientPC);window.opera6_bugs=is_opera_preseven;window.opera7_bugs=is_opera_seven&&!is_opera_95;window.opera95_bugs=/opera\/(9\.5)/.test(clientPC);}window.ie6_bugs=false;if(/msie ([0-9]{1,}[\.0-9]{0,})/.exec(clientPC)!=null&&parseFloat(RegExp.$1)<=6.0){ie6_bugs=true;}window.doneOnloadHook=undefined;if(!window.onloadFuncts){window.onloadFuncts=[];}window.addOnloadHook=function(hookFunct){if(!doneOnloadHook){onloadFuncts[onloadFuncts.length]=hookFunct;}else{hookFunct();}};window.importScript=
function(page){var uri=mw.config.get('wgScript')+'?title='+mw.util.wikiUrlencode(page)+'&action=raw&ctype=text/javascript';return importScriptURI(uri);};window.loadedScripts={};window.importScriptURI=function(url){if(loadedScripts[url]){return null;}loadedScripts[url]=true;var s=document.createElement('script');s.setAttribute('src',url);s.setAttribute('type','text/javascript');document.getElementsByTagName('head')[0].appendChild(s);return s;};window.importStylesheet=function(page){return importStylesheetURI(mw.config.get('wgScript')+'?action=raw&ctype=text/css&title='+mw.util.wikiUrlencode(page));};window.importStylesheetURI=function(url,media){var l=document.createElement('link');l.type='text/css';l.rel='stylesheet';l.href=url;if(media){l.media=media;}document.getElementsByTagName('head')[0].appendChild(l);return l;};window.appendCSS=function(text){var s=document.createElement('style');s.type='text/css';s.rel='stylesheet';if(s.styleSheet){s.styleSheet.cssText=text;}else{s.appendChild(
document.createTextNode(text+''));}document.getElementsByTagName('head')[0].appendChild(s);return s;};var skinpath=mw.config.get('stylepath')+'/'+mw.config.get('skin');if(mw.config.get('skin')==='monobook'){if(opera6_bugs){importStylesheetURI(skinpath+'/Opera6Fixes.css');}else if(opera7_bugs){importStylesheetURI(skinpath+'/Opera7Fixes.css');}else if(opera95_bugs){importStylesheetURI(skinpath+'/Opera9Fixes.css');}else if(ff2_bugs){importStylesheetURI(skinpath+'/FF2Fixes.css');}}if(mw.config.get('wgBreakFrames')){if(window.top!=window){window.top.location=window.location;}}window.changeText=function(el,newText){if(el.innerText){el.innerText=newText;}else if(el.firstChild&&el.firstChild.nodeValue){el.firstChild.nodeValue=newText;}};window.killEvt=function(evt){evt=evt||window.event||window.Event;if(typeof(evt.preventDefault)!='undefined'){evt.preventDefault();evt.stopPropagation();}else{evt.cancelBubble=true;}return false;};window.mwEditButtons=[];window.mwCustomEditButtons=[];window.
escapeQuotes=function(text){var re=new RegExp("'","g");text=text.replace(re,"\\'");re=new RegExp("\\n","g");text=text.replace(re,"\\n");return escapeQuotesHTML(text);};window.escapeQuotesHTML=function(text){var re=new RegExp('&',"g");text=text.replace(re,"&amp;");re=new RegExp('"',"g");text=text.replace(re,"&quot;");re=new RegExp('<',"g");text=text.replace(re,"&lt;");re=new RegExp('>',"g");text=text.replace(re,"&gt;");return text;};window.tooltipAccessKeyPrefix='alt-';if(is_opera){tooltipAccessKeyPrefix='shift-esc-';}else if(is_chrome){tooltipAccessKeyPrefix=is_chrome_mac?'ctrl-option-':'alt-';}else if(!is_safari_win&&is_safari&&webkit_version>526){tooltipAccessKeyPrefix='ctrl-alt-';}else if(!is_safari_win&&(is_safari||clientPC.indexOf('mac')!=-1||clientPC.indexOf('konqueror')!=-1)){tooltipAccessKeyPrefix='ctrl-';}else if(is_ff2){tooltipAccessKeyPrefix='alt-shift-';}window.tooltipAccessKeyRegexp=/\[(ctrl-)?(alt-)?(shift-)?(esc-)?(.)\]$/;window.updateTooltipAccessKeys=function(nodeList)
{if(!nodeList){var linkContainers=['column-one','mw-head','mw-panel','p-logo'];for(var i in linkContainers){var linkContainer=document.getElementById(linkContainers[i]);if(linkContainer){updateTooltipAccessKeys(linkContainer.getElementsByTagName('a'));}}updateTooltipAccessKeys(document.getElementsByTagName('input'));updateTooltipAccessKeys(document.getElementsByTagName('label'));return;}for(var i=0;i<nodeList.length;i++){var element=nodeList[i];var tip=element.getAttribute('title');if(tip&&tooltipAccessKeyRegexp.exec(tip)){tip=tip.replace(tooltipAccessKeyRegexp,'['+tooltipAccessKeyPrefix+"$5]");element.setAttribute('title',tip);}}};window.addPortletLink=function(portlet,href,text,id,tooltip,accesskey,nextnode){var root=document.getElementById(portlet);if(!root){return null;}var uls=root.getElementsByTagName('ul');var node;if(uls.length>0){node=uls[0];}else{node=document.createElement('ul');var lastElementChild=null;for(var i=0;i<root.childNodes.length;++i){if(root.childNodes[i].
nodeType==1){lastElementChild=root.childNodes[i];}}if(lastElementChild&&lastElementChild.nodeName.match(/div/i)){lastElementChild.appendChild(node);}else{root.appendChild(node);}}if(!node){return null;}root.className=root.className.replace(/(^| )emptyPortlet( |$)/,"$2");var link=document.createElement('a');link.appendChild(document.createTextNode(text));link.href=href;var span=document.createElement('span');span.appendChild(link);var item=document.createElement('li');item.appendChild(span);if(id){item.id=id;}if(accesskey){link.setAttribute('accesskey',accesskey);tooltip+=' ['+accesskey+']';}if(tooltip){link.setAttribute('title',tooltip);}if(accesskey&&tooltip){updateTooltipAccessKeys([link]);}if(nextnode&&nextnode.parentNode==node){node.insertBefore(item,nextnode);}else{node.appendChild(item);}return item;};window.getInnerText=function(el){if(typeof el=='string'){return el;}if(typeof el=='undefined'){return el;}if(el.nodeType&&el.getAttribute('data-sort-value')!==null){return el.
getAttribute('data-sort-value');}if(el.textContent){return el.textContent;}if(el.innerText){return el.innerText;}var str='';var cs=el.childNodes;var l=cs.length;for(var i=0;i<l;i++){switch(cs[i].nodeType){case 1:str+=getInnerText(cs[i]);break;case 3:str+=cs[i].nodeValue;break;}}return str;};window.checkboxes=undefined;window.lastCheckbox=undefined;window.setupCheckboxShiftClick=function(){checkboxes=[];lastCheckbox=null;var inputs=document.getElementsByTagName('input');addCheckboxClickHandlers(inputs);};window.addCheckboxClickHandlers=function(inputs,start){if(!start){start=0;}var finish=start+250;if(finish>inputs.length){finish=inputs.length;}for(var i=start;i<finish;i++){var cb=inputs[i];if(!cb.type||cb.type.toLowerCase()!='checkbox'||(' '+cb.className+' ').indexOf(' noshiftselect ')!=-1){continue;}var end=checkboxes.length;checkboxes[end]=cb;cb.index=end;addClickHandler(cb,checkboxClickHandler);}if(finish<inputs.length){setTimeout(function(){addCheckboxClickHandlers(inputs,finish);}
,200);}};window.checkboxClickHandler=function(e){if(typeof e=='undefined'){e=window.event;}if(!e.shiftKey||lastCheckbox===null){lastCheckbox=this.index;return true;}var endState=this.checked;var start,finish;if(this.index<lastCheckbox){start=this.index+1;finish=lastCheckbox;}else{start=lastCheckbox;finish=this.index-1;}for(var i=start;i<=finish;++i){checkboxes[i].checked=endState;if(i>start&&typeof checkboxes[i].onchange=='function'){checkboxes[i].onchange();}}lastCheckbox=this.index;return true;};window.getElementsByClassName=function(oElm,strTagName,oClassNames){var arrReturnElements=[];if(typeof(oElm.getElementsByClassName)=='function'){var arrNativeReturn=oElm.getElementsByClassName(oClassNames);if(strTagName=='*'){return arrNativeReturn;}for(var h=0;h<arrNativeReturn.length;h++){if(arrNativeReturn[h].tagName.toLowerCase()==strTagName.toLowerCase()){arrReturnElements[arrReturnElements.length]=arrNativeReturn[h];}}return arrReturnElements;}var arrElements=(strTagName=='*'&&oElm.all)
?oElm.all:oElm.getElementsByTagName(strTagName);var arrRegExpClassNames=[];if(typeof oClassNames=='object'){for(var i=0;i<oClassNames.length;i++){arrRegExpClassNames[arrRegExpClassNames.length]=new RegExp("(^|\\s)"+oClassNames[i].replace(/\-/g,"\\-")+"(\\s|$)");}}else{arrRegExpClassNames[arrRegExpClassNames.length]=new RegExp("(^|\\s)"+oClassNames.replace(/\-/g,"\\-")+"(\\s|$)");}var oElement;var bMatchesAll;for(var j=0;j<arrElements.length;j++){oElement=arrElements[j];bMatchesAll=true;for(var k=0;k<arrRegExpClassNames.length;k++){if(!arrRegExpClassNames[k].test(oElement.className)){bMatchesAll=false;break;}}if(bMatchesAll){arrReturnElements[arrReturnElements.length]=oElement;}}return(arrReturnElements);};window.redirectToFragment=function(fragment){var match=navigator.userAgent.match(/AppleWebKit\/(\d+)/);if(match){var webKitVersion=parseInt(match[1]);if(webKitVersion<420){return;}}if(window.location.hash==''){window.location.hash=fragment;if(is_gecko){addOnloadHook(function(){if(
window.location.hash==fragment){window.location.hash=fragment;}});}}};window.jsMsg=function(message,className){if(!document.getElementById){return false;}var messageDiv=document.getElementById('mw-js-message');if(!messageDiv){messageDiv=document.createElement('div');if(document.getElementById('column-content')&&document.getElementById('content')){document.getElementById('content').insertBefore(messageDiv,document.getElementById('content').firstChild);}else if(document.getElementById('content')&&document.getElementById('article')){document.getElementById('article').insertBefore(messageDiv,document.getElementById('article').firstChild);}else{return false;}}messageDiv.setAttribute('id','mw-js-message');messageDiv.style.display='block';if(className){messageDiv.setAttribute('class','mw-js-message-'+className);}if(typeof message==='object'){while(messageDiv.hasChildNodes()){messageDiv.removeChild(messageDiv.firstChild);}messageDiv.appendChild(message);}else{messageDiv.innerHTML=message;}
return true;};window.injectSpinner=function(element,id){var spinner=document.createElement('img');spinner.id='mw-spinner-'+id;spinner.src=mw.config.get('stylepath')+'/common/images/spinner.gif';spinner.alt=spinner.title='...';if(element.nextSibling){element.parentNode.insertBefore(spinner,element.nextSibling);}else{element.parentNode.appendChild(spinner);}};window.removeSpinner=function(id){var spinner=document.getElementById('mw-spinner-'+id);if(spinner){spinner.parentNode.removeChild(spinner);}};window.runOnloadHook=function(){if(doneOnloadHook||!(document.getElementById&&document.getElementsByTagName)){return;}doneOnloadHook=true;for(var i=0;i<onloadFuncts.length;i++){onloadFuncts[i]();}};window.addHandler=function(element,attach,handler){if(element.addEventListener){element.addEventListener(attach,handler,false);}else if(element.attachEvent){element.attachEvent('on'+attach,handler);}};window.hookEvent=function(hookName,hookFunct){addHandler(window,hookName,hookFunct);};window.
addClickHandler=function(element,handler){addHandler(element,'click',handler);};window.removeHandler=function(element,remove,handler){if(window.removeEventListener){element.removeEventListener(remove,handler,false);}else if(window.detachEvent){element.detachEvent('on'+remove,handler);}};hookEvent('load',runOnloadHook);if(ie6_bugs){importScriptURI(mw.config.get('stylepath')+'/common/IEFixes.js');}})();;},{},{});mw.loader.implement("mediawiki.page.startup",function($){(function($){mw.page={};$('html').addClass('client-js').removeClass('client-nojs');$(mw.util.init);})(jQuery);;},{},{});mw.loader.implement("mediawiki.util",function($){(function($,mw){"use strict";var util={init:function(){var profile,$tocTitle,$tocToggleLink,hideTocCookie;$.messageBoxNew({id:'mw-js-message',parent:'#content'});profile=$.client.profile();if(profile.name==='opera'){util.tooltipAccessKeyPrefix='shift-esc-';}else if(profile.name==='chrome'){util.tooltipAccessKeyPrefix=(profile.platform==='mac'?'ctrl-option-':
profile.platform==='win'?'alt-shift-':'alt-');}else if(profile.platform!=='win'&&profile.name==='safari'&&profile.layoutVersion>526){util.tooltipAccessKeyPrefix='ctrl-alt-';}else if(!(profile.platform==='win'&&profile.name==='safari')&&(profile.name==='safari'||profile.platform==='mac'||profile.name==='konqueror')){util.tooltipAccessKeyPrefix='ctrl-';}else if(profile.name==='firefox'&&profile.versionBase>'1'){util.tooltipAccessKeyPrefix='alt-shift-';}if($('#bodyContent').length){util.$content=$('#bodyContent');}else if($('#mw_contentholder').length){util.$content=$('#mw_contentholder');}else if($('#article').length){util.$content=$('#article');}else{util.$content=$('#content');}$tocTitle=$('#toctitle');$tocToggleLink=$('#togglelink');if($('#toc').length&&$tocTitle.length&&!$tocToggleLink.length){hideTocCookie=$.cookie('mw_hidetoc');$tocToggleLink=$('<a href="#" class="internal" id="togglelink"></a>').text(mw.msg('hidetoc')).click(function(e){e.preventDefault();util.toggleToc($(this));}
);$tocTitle.append($tocToggleLink.wrap('<span class="toctoggle"></span>').parent().prepend('&nbsp;[').append(']&nbsp;'));if(hideTocCookie==='1'){util.toggleToc($tocToggleLink);}}},rawurlencode:function(str){str=String(str);return encodeURIComponent(str).replace(/!/g,'%21').replace(/'/g,'%27').replace(/\(/g,'%28').replace(/\)/g,'%29').replace(/\*/g,'%2A').replace(/~/g,'%7E');},wikiUrlencode:function(str){return util.rawurlencode(str).replace(/%20/g,'_').replace(/%3A/g,':').replace(/%2F/g,'/');},wikiGetlink:function(str){return mw.config.get('wgArticlePath').replace('$1',util.wikiUrlencode(typeof str==='string'?str:mw.config.get('wgPageName')));},wikiScript:function(str){return mw.config.get('wgScriptPath')+'/'+(str||'index')+mw.config.get('wgScriptExtension');},addCSS:function(text){var s=document.createElement('style');s.type='text/css';s.rel='stylesheet';document.getElementsByTagName('head')[0].appendChild(s);if(s.styleSheet){s.styleSheet.cssText=text;}else{s.appendChild(document.
createTextNode(String(text)));}return s.sheet||s;},toggleToc:function($toggleLink,callback){var $tocList=$('#toc ul:first');if($tocList.length){if($tocList.is(':hidden')){$tocList.slideDown('fast',callback);$toggleLink.text(mw.msg('hidetoc'));$('#toc').removeClass('tochidden');$.cookie('mw_hidetoc',null,{expires:30,path:'/'});return true;}else{$tocList.slideUp('fast',callback);$toggleLink.text(mw.msg('showtoc'));$('#toc').addClass('tochidden');$.cookie('mw_hidetoc','1',{expires:30,path:'/'});return false;}}else{return null;}},getParamValue:function(param,url){url=url||document.location.href;var re=new RegExp('^[^#]*[&?]'+$.escapeRE(param)+'=([^&#]*)'),m=re.exec(url);if(m&&m.length>1){return decodeURIComponent(m[1].replace(/\+/g,'%20'));}return null;},tooltipAccessKeyPrefix:'alt-',tooltipAccessKeyRegexp:/\[(ctrl-)?(alt-)?(shift-)?(esc-)?(.)\]$/,updateTooltipAccessKeys:function($nodes){if(!$nodes){$nodes=$('#column-one a, #mw-head a, #mw-panel a, #p-logo a, input, label');}else if(!(
$nodes instanceof $)){$nodes=$($nodes);}$nodes.attr('title',function(i,val){if(val&&util.tooltipAccessKeyRegexp.exec(val)){return val.replace(util.tooltipAccessKeyRegexp,'['+util.tooltipAccessKeyPrefix+'$5]');}return val;});},$content:null,addPortletLink:function(portlet,href,text,id,tooltip,accesskey,nextnode){var $item,$link,$portlet,$ul;if(arguments.length<3){return null;}$link=$('<a>').attr('href',href).text(text);if(tooltip){$link.attr('title',tooltip);}switch(mw.config.get('skin')){case'standard':case'cologneblue':$('#quickbar').append($link.after('<br/>'));return $link[0];case'nostalgia':$('#searchform').before($link).before(' &#124; ');return $link[0];default:$portlet=$('#'+portlet);if($portlet.length===0){return null;}$ul=$portlet.find('ul');if($ul.length===0){if($portlet.find('div:first').length===0){$portlet.append('<ul></ul>');}else{$portlet.find('div').eq(-1).append('<ul></ul>');}$ul=$portlet.find('ul').eq(0);}if($ul.length===0){return null;}$portlet.removeClass(
'emptyPortlet');if($portlet.hasClass('vectorTabs')){$item=$link.wrap('<li><span></span></li>').parent().parent();}else{$item=$link.wrap('<li></li>').parent();}if(id){$item.attr('id',id);}if(accesskey){$link.attr('accesskey',accesskey);tooltip+=' ['+accesskey+']';$link.attr('title',tooltip);}if(accesskey&&tooltip){util.updateTooltipAccessKeys($link);}if(nextnode&&nextnode.parentNode===$ul[0]){$(nextnode).before($item);}else if(typeof nextnode==='string'&&$ul.find(nextnode).length!==0){$ul.find(nextnode).eq(0).before($item);}else{$ul.append($item);}return $item[0];}},jsMessage:function(message,className){if(!arguments.length||message===''||message===null){$('#mw-js-message').empty().hide();return true;}else{var $messageDiv=$('#mw-js-message');if(!$messageDiv.length){$messageDiv=$('<div id="mw-js-message"></div>');if(util.$content.parent().length){util.$content.parent().prepend($messageDiv);}else{return false;}}if(className){$messageDiv.prop('class','mw-js-message-'+className);}if(typeof
message==='object'){$messageDiv.empty();$messageDiv.append(message);}else{$messageDiv.html(message);}$messageDiv.slideDown();return true;}},validateEmail:function(mailtxt){var rfc5322_atext,rfc1034_ldh_str,HTML5_email_regexp;if(mailtxt===''){return null;}rfc5322_atext="a-z0-9!#$%&'*+\\-/=?^_`{|}~";rfc1034_ldh_str="a-z0-9\\-";HTML5_email_regexp=new RegExp('^'+'['+rfc5322_atext+'\\.]+'+'@'+'['+rfc1034_ldh_str+']+'+'(?:\\.['+rfc1034_ldh_str+']+)*'+'$','i');return(null!==mailtxt.match(HTML5_email_regexp));},isIPv4Address:function(address,allowBlock){if(typeof address!=='string'){return false;}var block=allowBlock?'(?:\\/(?:3[0-2]|[12]?\\d))?':'',RE_IP_BYTE='(?:25[0-5]|2[0-4][0-9]|1[0-9][0-9]|0?[0-9]?[0-9])',RE_IP_ADD='(?:'+RE_IP_BYTE+'\\.){3}'+RE_IP_BYTE;return address.search(new RegExp('^'+RE_IP_ADD+block+'$'))!==-1;},isIPv6Address:function(address,allowBlock){if(typeof address!=='string'){return false;}var block=allowBlock?'(?:\\/(?:12[0-8]|1[01][0-9]|[1-9]?\\d))?':'',RE_IPV6_ADD='(?:'+
':(?::|(?::'+'[0-9A-Fa-f]{1,4}'+'){1,7})'+'|'+'[0-9A-Fa-f]{1,4}'+'(?::'+'[0-9A-Fa-f]{1,4}'+'){0,6}::'+'|'+'[0-9A-Fa-f]{1,4}'+'(?::'+'[0-9A-Fa-f]{1,4}'+'){7}'+')';if(address.search(new RegExp('^'+RE_IPV6_ADD+block+'$'))!==-1){return true;}RE_IPV6_ADD='[0-9A-Fa-f]{1,4}'+'(?:::?'+'[0-9A-Fa-f]{1,4}'+'){1,6}';return address.search(new RegExp('^'+RE_IPV6_ADD+block+'$'))!==-1&&address.search(/::/)!==-1&&address.search(/::.*::/)===-1;}};mw.util=util;})(jQuery,mediaWiki);;},{},{"showtoc":"show","hidetoc":"hide"});

/* cache key: wikidb-mw_:resourceloader:filter:minify-js:7:6c294bc637a7ae49fc790cbccf7540cd */
