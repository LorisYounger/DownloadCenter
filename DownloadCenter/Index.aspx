<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Index.aspx.cs" Inherits="DownloadCenter.Index" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <title>Download Center</title>
    <link href="StyleSheet.css" rel="stylesheet" type="text/css" />
    <script>
        function HTTP_REFERER() {
            history.back();
        }
    </script>
</head>
<body>
    <div id="MainForm">
        <div class="form_boxA">
            <h1 id="h1title" runat="server"></h1>
            <br />
            <p>当前目录是 <b id="CurrentFolder" runat="server" /></p>

            <table cellpadding="0" cellspacing="0">
                <tr>
                    <th width="20">序号</th>
                    <th>名称</th>
                    <th>描述</th>
                    <th width="200">修改日期</th>
                    <th width="50">类型</th>
                    <th width="100">大小</th>
                </tr>
                <asp:Literal ID="Table" runat="server"></asp:Literal>
            </table>
            <br />
            <h2 id="h2readme" runat="server"></h2>
            <asp:Literal ID="ReadMe" runat="server"></asp:Literal>
            <br />
        </div>
    </div>
</body>
</html>
