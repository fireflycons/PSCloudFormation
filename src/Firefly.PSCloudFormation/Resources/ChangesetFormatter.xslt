<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl"
>
    <xsl:template match="/Stacks">
        <html>
            <head>
                <title>Changeset Detail</title>

                <style>
                    body {
                    font-family: arial, sans-serif;
                    font-size: 12px;
                    color: white;
                    background-color: black;
                    }
                    table {
                    font-size: 12px;
                    }
                    table.change-summary {
                    border: 1px solid #dddddd;
                    border-collapse: collapse;
                    width: 100%;
                    }
                    table.vert {
                    font-family: arial, sans-serif;
                    border-collapse: collapse;
                    }
                    td, th {
                    text-align: left;
                    padding: 4px;
                    }
                    td.yellow {
                    color: yellow;
                    }
                    td.red {
                    color: red;
                    }
                    td.cyan {
                    color: cyan;
                    }
                    td.green {
                    color: lightgreen;
                    }
                    td.left {
                    width: 150px;
                    }
                    .column {
                    float: left;
                    padding: 10px;
                    }
                    .left {
                    width: 25%;
                    }
                    .right {
                    width: 75%;
                    }
                    .row:after {
                    content: "";
                    display: table;
                    clear: both;
                    }
                </style>
            </head>
            <body>
                <h1>Detailed Changeset Information</h1>
                <xsl:for-each select="Stack">
                    <table class="vert">
                        <tr>
                            <td class="left">Stack Name</td>
                            <td>
                                <xsl:value-of select="StackName"/>
                            </td>
                        </tr>
                        <tr>
                            <td class="left">Changeset Name</td>
                            <td>
                                <xsl:value-of select="ChangeSetName"/>
                            </td>
                        </tr>
                        <xsl:if test="Description != ''">
                            <tr>
                                <td class="left">Description</td>
                                <td>
                                    <xsl:value-of select="Description"/>
                                </td>
                            </tr>
                        </xsl:if>
                        <tr>
                            <td class="left">CreationTime</td>
                            <td>
                                <xsl:value-of select="CreationTime"/>
                            </td>
                        </tr>
                    </table>
                    <div class="row">
                        <div class="column">
                            <br/>
                        </div>
                        <div class="column">
                            <h2>Change Details</h2>
                            <xsl:for-each select="Changes">
                                <table class="change-summary">
                                    <th>Action</th>
                                    <th>Logical Id</th>
                                    <th>Type</th>
                                    <th>Replacement</th>
                                    <th>Physical ID</th>
                                    <tr>
                                        <xsl:choose>
                                            <xsl:when test="ResourceChange/Action = 'Add'">
                                                <td class="green">
                                                    <xsl:value-of select="ResourceChange/Action"/>
                                                </td>
                                            </xsl:when>
                                            <xsl:when test="ResourceChange/Action = 'Modify'">
                                                <td class="yellow">
                                                    <xsl:value-of select="ResourceChange/Action"/>
                                                </td>
                                            </xsl:when>
                                            <xsl:when test="ResourceChange/Action = 'Remove'">
                                                <td class="Red">
                                                    <xsl:value-of select="ResourceChange/Action"/>
                                                </td>
                                            </xsl:when>
                                            <xsl:when test="ResourceChange/Action = 'Import'">
                                                <td class="Cyan">
                                                    <xsl:value-of select="ResourceChange/Action"/>
                                                </td>
                                            </xsl:when>
                                            <xsl:otherwise>
                                                <td>
                                                    <xsl:value-of select="ResourceChange/Action"/>
                                                </td>
                                            </xsl:otherwise>
                                        </xsl:choose>
                                        <td>
                                            <xsl:value-of select="ResourceChange/LogicalResourceId"/>
                                        </td>
                                        <td>
                                            <xsl:value-of select="ResourceChange/ResourceType"/>
                                        </td>
                                        <xsl:choose>
                                            <xsl:when test="ResourceChange/Replacement = 'False'">
                                                <td class="green">
                                                    <xsl:value-of select="ResourceChange/Replacement"/>
                                                </td>
                                            </xsl:when>
                                            <xsl:when test="ResourceChange/Replacement = 'Conditional'">
                                                <td class="yellow">
                                                    <xsl:value-of select="ResourceChange/Replacement"/>
                                                </td>
                                            </xsl:when>
                                            <xsl:when test="ResourceChange/Replacement = 'True'">
                                                <td class="Red">
                                                    <xsl:value-of select="ResourceChange/Replacement"/>
                                                </td>
                                            </xsl:when>
                                            <xsl:otherwise>
                                                <td>
                                                    <xsl:value-of select="ResourceChange/Replacement"/>
                                                </td>
                                            </xsl:otherwise>
                                        </xsl:choose>
                                        <td>
                                            <xsl:value-of select="ResourceChange/PhysicalResourceId"/>
                                        </td>
                                    </tr>
                                </table>
                                <xsl:if test="ResourceChange/Scope != ''">
                                    <h3>Scope</h3>
                                    <ul>
                                    <xsl:choose>
                                        <xsl:when test="ResourceChange/Scope/Item != ''">
                                            <xsl:for-each select="ResourceChange/Scope/Item">
                                                <li><xsl:value-of select="."/></li>
                                            </xsl:for-each>
                                        </xsl:when>
                                        <xsl:otherwise>
                                            <li>
                                                <xsl:value-of select="ResourceChange/Scope"/>
                                            </li>
                                        </xsl:otherwise>
                                    </xsl:choose>
                                    </ul>
                                </xsl:if>
                                <h3>Change Detail</h3>
                                <ol>
                                    <xsl:for-each select="ResourceChange/Details">
                                        <li>
                                            <span>
                                                <h3>Source</h3>
                                                <table class="vert">
                                                    <tr>
                                                        <td class="left">Causing Entity</td>
                                                        <xsl:choose>
                                                            <xsl:when test="CausingEntity = ''">
                                                                <td>
                                                                    <i>null</i>
                                                                </td>
                                                            </xsl:when>
                                                            <xsl:otherwise>
                                                                <td>
                                                                    <xsl:value-of select="CausingEntity"/>
                                                                </td>
                                                            </xsl:otherwise>
                                                        </xsl:choose>
                                                    </tr>
                                                    <tr>
                                                        <td class="left">Change Source</td>
                                                        <td>
                                                            <xsl:value-of select="ChangeSource"/>
                                                        </td>
                                                    </tr>
                                                    <tr>
                                                        <td class="left">Evaluation</td>
                                                        <td>
                                                            <xsl:value-of select="Evaluation"/>
                                                        </td>
                                                    </tr>
                                                </table>
                                                <h3>Target</h3>
                                                <table class="vert">
                                                    <tr>
                                                        <td class="left">Attribute</td>
                                                        <td>
                                                            <xsl:value-of select="Target/Attribute"/>
                                                        </td>
                                                    </tr>
                                                    <tr>
                                                        <td class="left">Name</td>
                                                        <td>
                                                            <xsl:value-of select="Target/Name"/>
                                                        </td>
                                                    </tr>
                                                    <tr>
                                                        <td class="left">Requires Recreation</td>
                                                        <xsl:choose>
                                                            <xsl:when test="Target/RequiresRecreation = 'Never'">
                                                                <td class="green">
                                                                    <xsl:value-of select="Target/RequiresRecreation"/>
                                                                </td>
                                                            </xsl:when>
                                                            <xsl:when test="Target/RequiresRecreation = 'Conditionally'">
                                                                <td class="yellow">
                                                                    <xsl:value-of select="Target/RequiresRecreation"/>
                                                                </td>
                                                            </xsl:when>
                                                            <xsl:when test="Target/RequiresRecreation = 'Always'">
                                                                <td class="Red">
                                                                    <xsl:value-of select="Target/RequiresRecreation"/>
                                                                </td>
                                                            </xsl:when>
                                                            <xsl:otherwise>
                                                                <td>
                                                                    <xsl:value-of select="Target/RequiresRecreation"/>
                                                                </td>
                                                            </xsl:otherwise>
                                                        </xsl:choose>
                                                    </tr>
                                                </table>
                                            </span>
                                        </li>
                                    </xsl:for-each>
                                </ol>
                            </xsl:for-each>
                        </div>
                    </div>
                </xsl:for-each>
            </body>
        </html>
    </xsl:template>
</xsl:stylesheet>
