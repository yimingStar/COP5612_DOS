(function(Global)
{
 "use strict";
 var twitter_client,Client,_twitterclient_Templates,WebSharper,UI,AttrProxy,IntelliFactory,Runtime,Doc,Var$1,View,Strings,Arrays,List,Input,Mouse,Concurrency,Remoting,AjaxRemotingProvider,Templating,Runtime$1,Server,ProviderBuilder,Handler,TemplateInstance,Client$1,Templates;
 twitter_client=Global.twitter_client=Global.twitter_client||{};
 Client=twitter_client.Client=twitter_client.Client||{};
 _twitterclient_Templates=Global["twitter-client_Templates"]=Global["twitter-client_Templates"]||{};
 WebSharper=Global.WebSharper;
 UI=WebSharper&&WebSharper.UI;
 AttrProxy=UI&&UI.AttrProxy;
 IntelliFactory=Global.IntelliFactory;
 Runtime=IntelliFactory&&IntelliFactory.Runtime;
 Doc=UI&&UI.Doc;
 Var$1=UI&&UI.Var$1;
 View=UI&&UI.View;
 Strings=WebSharper&&WebSharper.Strings;
 Arrays=WebSharper&&WebSharper.Arrays;
 List=WebSharper&&WebSharper.List;
 Input=UI&&UI.Input;
 Mouse=Input&&Input.Mouse;
 Concurrency=WebSharper&&WebSharper.Concurrency;
 Remoting=WebSharper&&WebSharper.Remoting;
 AjaxRemotingProvider=Remoting&&Remoting.AjaxRemotingProvider;
 Templating=UI&&UI.Templating;
 Runtime$1=Templating&&Templating.Runtime;
 Server=Runtime$1&&Runtime$1.Server;
 ProviderBuilder=Server&&Server.ProviderBuilder;
 Handler=Server&&Server.Handler;
 TemplateInstance=Server&&Server.TemplateInstance;
 Client$1=UI&&UI.Client;
 Templates=Client$1&&Client$1.Templates;
 Client.ResponseComponet=function()
 {
  var rvText,inputField,view,viewCaps,viewReverse,viewWordCount,views;
  function cls(s)
  {
   return AttrProxy.Create("class",s);
  }
  function a(x,y)
  {
   return(((Runtime.Curried3(function($1,$2,$3)
   {
    return $1(Global.String($2)+":"+Global.String($3));
   }))(Global.id))(y))(x);
  }
  function tableRow(lbl,view$1)
  {
   return Doc.Element("tr",[],[Doc.Element("td",[],[Doc.TextNode(lbl)]),Doc.Element("td",[AttrProxy.Create("style","width:66%")],[Doc.TextView(view$1)])]);
  }
  rvText=Var$1.Create$1("");
  inputField=Doc.Element("div",[cls("panel panel-default")],[Doc.Element("div",[cls("panel-heading")],[Doc.Element("h3",[cls("panel-title")],[Doc.TextNode("Input")])]),Doc.Element("div",[cls("panel-body")],[Doc.Element("form",[cls("form-horizontal"),AttrProxy.Create("role","form")],[Doc.Element("div",[cls("form-group")],[Doc.Element("label",[cls("col-sm-4 control-label"),AttrProxy.Create("for","inputBox")],[Doc.TextNode("Write something: ")]),Doc.Element("div",[cls("col-sm-8")],[Doc.Input([cls("form-control"),AttrProxy.Create("id","inputBox")],rvText)])])])])]);
  view=rvText.get_View();
  viewCaps=View.Map(function(s)
  {
   return s.toUpperCase();
  },view);
  viewReverse=View.Map(function(s)
  {
   return Strings.ToCharArray(s).slice().reverse().join("");
  },view);
  viewWordCount=View.Map(function(s)
  {
   return Arrays.length(Strings.SplitChars(s,[" "],0));
  },view);
  views=List.ofArray([["Entered Text",view],["Capitalised",viewCaps],["Reversed",viewReverse],["Word Count",View.Map(Global.String,viewWordCount)],["Word count odd or even?",View.Map(function(i)
  {
   return i%2===0?"Even":"Odd";
  },viewWordCount)],["Mouse coordinates",View.Map(function($1)
  {
   return a($1[0],$1[1]);
  },Mouse.get_Position())]]);
  return Doc.Element("div",[],[inputField,Doc.Element("div",[cls("panel panel-default")],[Doc.Element("div",[cls("panel-heading")],[Doc.Element("h3",[cls("panel-title")],[Doc.TextNode("Output")])]),Doc.Element("div",[cls("panel-body")],[Doc.Element("table",[cls("table")],[Doc.Element("tbody",[],[Doc.Concat(List.map(function($1)
  {
   return tableRow($1[0],$1[1]);
  },views))])])])])]);
 };
 Client.TweetComponent$29$20=function(rvResponse)
 {
  return function(e)
  {
   var b;
   Concurrency.StartImmediate((b=null,Concurrency.Delay(function()
   {
    return Concurrency.Bind((new AjaxRemotingProvider.New()).Async("twitter-client:twitter_client.CallApi.SendTweet:-1794501434",[e.Vars.Hole("texttoreverse").$1.Get()]),function(a)
    {
     rvResponse.Set(a);
     return Concurrency.Zero();
    });
   })),null);
  };
 };
 Client.TweetComponent=function()
 {
  var rvResponse,b,R,_this,t,p,i;
  rvResponse=Var$1.Create$1("");
  return(b=(R=rvResponse.get_View(),(_this=(t=new ProviderBuilder.New$1(),(t.h.push(Handler.EventQ2(t.k,"onsend",function()
  {
   return t.i;
  },function(e)
  {
   var b$1;
   Concurrency.StartImmediate((b$1=null,Concurrency.Delay(function()
   {
    return Concurrency.Bind((new AjaxRemotingProvider.New()).Async("twitter-client:twitter_client.CallApi.SendTweet:-1794501434",[e.Vars.Hole("texttoreverse").$1.Get()]),function(a)
    {
     rvResponse.Set(a);
     return Concurrency.Zero();
    });
   })),null);
  })),t)),(_this.h.push({
   $:2,
   $0:"response",
   $1:R
  }),_this))),(p=Handler.CompleteHoles(b.k,b.h,[["texttoreverse",0]]),(i=new TemplateInstance.New(p[1],_twitterclient_Templates.mainform(p[0])),b.i=i,i))).get_Doc();
 };
 Client.SignInComponent$18$20=function()
 {
  return function(e)
  {
   var b;
   Concurrency.StartImmediate((b=null,Concurrency.Delay(function()
   {
    (new AjaxRemotingProvider.New()).Async("twitter-client:twitter_client.CallApi.RequestSignIn:-1794501434",[e.Vars.Hole("textuserid").$1.Get()]);
    return Concurrency.Zero();
   })),null);
  };
 };
 Client.SignInComponent=function()
 {
  var b,t,p,i;
  return(b=(t=new ProviderBuilder.New$1(),(t.h.push(Handler.EventQ2(t.k,"onsend",function()
  {
   return t.i;
  },function(e)
  {
   var b$1;
   Concurrency.StartImmediate((b$1=null,Concurrency.Delay(function()
   {
    (new AjaxRemotingProvider.New()).Async("twitter-client:twitter_client.CallApi.RequestSignIn:-1794501434",[e.Vars.Hole("textuserid").$1.Get()]);
    return Concurrency.Zero();
   })),null);
  })),t)),(p=Handler.CompleteHoles(b.k,b.h,[["textuserid",0]]),(i=new TemplateInstance.New(p[1],_twitterclient_Templates.signinform(p[0])),b.i=i,i))).get_Doc();
 };
 _twitterclient_Templates.mainform=function(h)
 {
  Templates.LoadLocalTemplates("main");
  return h?Templates.NamedTemplate("main",{
   $:1,
   $0:"mainform"
  },h):void 0;
 };
 _twitterclient_Templates.signinform=function(h)
 {
  Templates.LoadLocalTemplates("main");
  return h?Templates.NamedTemplate("main",{
   $:1,
   $0:"signinform"
  },h):void 0;
 };
}(self));
