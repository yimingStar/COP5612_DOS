(function(Global)
{
 "use strict";
 var twitter_client,Client,_twitterclient_Templates,WebSharper,UI,Var$1,Templating,Runtime,Server,ProviderBuilder,Handler,TemplateInstance,Concurrency,Remoting,AjaxRemotingProvider,Client$1,Templates;
 twitter_client=Global.twitter_client=Global.twitter_client||{};
 Client=twitter_client.Client=twitter_client.Client||{};
 _twitterclient_Templates=Global["twitter-client_Templates"]=Global["twitter-client_Templates"]||{};
 WebSharper=Global.WebSharper;
 UI=WebSharper&&WebSharper.UI;
 Var$1=UI&&UI.Var$1;
 Templating=UI&&UI.Templating;
 Runtime=Templating&&Templating.Runtime;
 Server=Runtime&&Runtime.Server;
 ProviderBuilder=Server&&Server.ProviderBuilder;
 Handler=Server&&Server.Handler;
 TemplateInstance=Server&&Server.TemplateInstance;
 Concurrency=WebSharper&&WebSharper.Concurrency;
 Remoting=WebSharper&&WebSharper.Remoting;
 AjaxRemotingProvider=Remoting&&Remoting.AjaxRemotingProvider;
 Client$1=UI&&UI.Client;
 Templates=Client$1&&Client$1.Templates;
 Client.BrowseTweetListComponent=function(inputList)
 {
  var b,R,_this,p,i;
  return(b=(R=Var$1.Create$1(inputList).get_View(),(_this=new ProviderBuilder.New$1(),(_this.h.push({
   $:2,
   $0:"response",
   $1:R
  }),_this))),(p=Handler.CompleteHoles(b.k,b.h,[]),(i=new TemplateInstance.New(p[1],_twitterclient_Templates.browsetweetlist(p[0])),b.i=i,i))).get_Doc();
 };
 Client.OwnTweetListComponent=function(inputList)
 {
  var b,R,_this,p,i;
  return(b=(R=Var$1.Create$1(inputList).get_View(),(_this=new ProviderBuilder.New$1(),(_this.h.push({
   $:2,
   $0:"response",
   $1:R
  }),_this))),(p=Handler.CompleteHoles(b.k,b.h,[]),(i=new TemplateInstance.New(p[1],_twitterclient_Templates.owntweetlist(p[0])),b.i=i,i))).get_Doc();
 };
 Client.SubscriberListComponent=function(inputList)
 {
  var b,R,_this,p,i;
  return(b=(R=Var$1.Create$1(inputList).get_View(),(_this=new ProviderBuilder.New$1(),(_this.h.push({
   $:2,
   $0:"response",
   $1:R
  }),_this))),(p=Handler.CompleteHoles(b.k,b.h,[]),(i=new TemplateInstance.New(p[1],_twitterclient_Templates.subscriberlist(p[0])),b.i=i,i))).get_Doc();
 };
 Client.SubscribedListComponent=function(inputList)
 {
  var b,R,_this,p,i;
  return(b=(R=Var$1.Create$1(inputList).get_View(),(_this=new ProviderBuilder.New$1(),(_this.h.push({
   $:2,
   $0:"response",
   $1:R
  }),_this))),(p=Handler.CompleteHoles(b.k,b.h,[]),(i=new TemplateInstance.New(p[1],_twitterclient_Templates.subscribedlist(p[0])),b.i=i,i))).get_Doc();
 };
 Client.SubscribeComponent$60$20=function(rvResponse)
 {
  return function(e)
  {
   var b;
   Concurrency.StartImmediate((b=null,Concurrency.Delay(function()
   {
    return Concurrency.Bind((new AjaxRemotingProvider.New()).Async("twitter-client:twitter_client.CallApi.Subscribe:-423149421",[e.Vars.Hole("textsubscribeid").$1.Get()]),function(a)
    {
     rvResponse.Set(a);
     return Concurrency.Zero();
    });
   })),null);
  };
 };
 Client.SubscribeComponent=function()
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
    return Concurrency.Bind((new AjaxRemotingProvider.New()).Async("twitter-client:twitter_client.CallApi.Subscribe:-423149421",[e.Vars.Hole("textsubscribeid").$1.Get()]),function(a)
    {
     rvResponse.Set(a);
     return Concurrency.Zero();
    });
   })),null);
  })),t)),(_this.h.push({
   $:2,
   $0:"response",
   $1:R
  }),_this))),(p=Handler.CompleteHoles(b.k,b.h,[["textsubscribeid",0]]),(i=new TemplateInstance.New(p[1],_twitterclient_Templates.subscribeform(p[0])),b.i=i,i))).get_Doc();
 };
 Client.TweetComponent$47$20=function(rvResponse)
 {
  return function(e)
  {
   var b;
   Concurrency.StartImmediate((b=null,Concurrency.Delay(function()
   {
    return Concurrency.Bind((new AjaxRemotingProvider.New()).Async("twitter-client:twitter_client.CallApi.SendTweet:-423149421",[e.Vars.Hole("texttweet").$1.Get()]),function(a)
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
    return Concurrency.Bind((new AjaxRemotingProvider.New()).Async("twitter-client:twitter_client.CallApi.SendTweet:-423149421",[e.Vars.Hole("texttweet").$1.Get()]),function(a)
    {
     rvResponse.Set(a);
     return Concurrency.Zero();
    });
   })),null);
  })),t)),(_this.h.push({
   $:2,
   $0:"response",
   $1:R
  }),_this))),(p=Handler.CompleteHoles(b.k,b.h,[["texttweet",0]]),(i=new TemplateInstance.New(p[1],_twitterclient_Templates.mainform(p[0])),b.i=i,i))).get_Doc();
 };
 Client.SignInComponent$34$20=function(rvResponse)
 {
  return function(e)
  {
   var b;
   Concurrency.StartImmediate((b=null,Concurrency.Delay(function()
   {
    return Concurrency.Bind((new AjaxRemotingProvider.New()).Async("twitter-client:twitter_client.CallApi.RequestSignIn:-423149421",[e.Vars.Hole("textuserid").$1.Get()]),function(a)
    {
     rvResponse.Set(a);
     return Concurrency.Zero();
    });
   })),null);
  };
 };
 Client.SignInComponent=function()
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
    return Concurrency.Bind((new AjaxRemotingProvider.New()).Async("twitter-client:twitter_client.CallApi.RequestSignIn:-423149421",[e.Vars.Hole("textuserid").$1.Get()]),function(a)
    {
     rvResponse.Set(a);
     return Concurrency.Zero();
    });
   })),null);
  })),t)),(_this.h.push({
   $:2,
   $0:"response",
   $1:R
  }),_this))),(p=Handler.CompleteHoles(b.k,b.h,[["textuserid",0]]),(i=new TemplateInstance.New(p[1],_twitterclient_Templates.signinform(p[0])),b.i=i,i))).get_Doc();
 };
 Client.SignUpComponent$21$20=function(rvResponse)
 {
  return function(e)
  {
   var b;
   Concurrency.StartImmediate((b=null,Concurrency.Delay(function()
   {
    return Concurrency.Bind((new AjaxRemotingProvider.New()).Async("twitter-client:twitter_client.CallApi.RequestRegister:-423149421",[e.Vars.Hole("account").$1.Get()]),function(a)
    {
     rvResponse.Set(a);
     return Concurrency.Zero();
    });
   })),null);
  };
 };
 Client.SignUpComponent=function()
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
    return Concurrency.Bind((new AjaxRemotingProvider.New()).Async("twitter-client:twitter_client.CallApi.RequestRegister:-423149421",[e.Vars.Hole("account").$1.Get()]),function(a)
    {
     rvResponse.Set(a);
     return Concurrency.Zero();
    });
   })),null);
  })),t)),(_this.h.push({
   $:2,
   $0:"response",
   $1:R
  }),_this))),(p=Handler.CompleteHoles(b.k,b.h,[["account",0]]),(i=new TemplateInstance.New(p[1],_twitterclient_Templates.registerform(p[0])),b.i=i,i))).get_Doc();
 };
 Client.UserInfoComponent=function(userData)
 {
  var b,U,_this,p,i;
  return(b=(U=Var$1.Create$1(userData).get_View(),(_this=new ProviderBuilder.New$1(),(_this.h.push({
   $:2,
   $0:"userinfo",
   $1:U
  }),_this))),(p=Handler.CompleteHoles(b.k,b.h,[]),(i=new TemplateInstance.New(p[1],_twitterclient_Templates.userdatainfo(p[0])),b.i=i,i))).get_Doc();
 };
 _twitterclient_Templates.owntweetlist=function(h)
 {
  Templates.LoadLocalTemplates("main");
  return h?Templates.NamedTemplate("main",{
   $:1,
   $0:"owntweetlist"
  },h):void 0;
 };
 _twitterclient_Templates.subscriberlist=function(h)
 {
  Templates.LoadLocalTemplates("main");
  return h?Templates.NamedTemplate("main",{
   $:1,
   $0:"subscriberlist"
  },h):void 0;
 };
 _twitterclient_Templates.subscribedlist=function(h)
 {
  Templates.LoadLocalTemplates("main");
  return h?Templates.NamedTemplate("main",{
   $:1,
   $0:"subscribedlist"
  },h):void 0;
 };
 _twitterclient_Templates.subscribeform=function(h)
 {
  Templates.LoadLocalTemplates("main");
  return h?Templates.NamedTemplate("main",{
   $:1,
   $0:"subscribeform"
  },h):void 0;
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
 _twitterclient_Templates.registerform=function(h)
 {
  Templates.LoadLocalTemplates("main");
  return h?Templates.NamedTemplate("main",{
   $:1,
   $0:"registerform"
  },h):void 0;
 };
 _twitterclient_Templates.userdatainfo=function(h)
 {
  Templates.LoadLocalTemplates("main");
  return h?Templates.NamedTemplate("main",{
   $:1,
   $0:"userdatainfo"
  },h):void 0;
 };
 _twitterclient_Templates.browsetweetlist=function(h)
 {
  Templates.LoadLocalTemplates("main");
  return h?Templates.NamedTemplate("main",{
   $:1,
   $0:"browsetweetlist"
  },h):void 0;
 };
}(self));
