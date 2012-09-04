<?php
require 'classes/openid.php';
require "head.php";
try {
    $openid = new LightOpenID;
    $openid->identity = 'https://steamcommunity.com/openid';

    if(!$openid->mode) {
        header('Location: ' . $openid->authUrl());
   	}elseif($openid->mode == 'cancel') {
	   //Cancled
       echo "Operation Cancelled";
    } else {
		if($openid->validate()){
			$id = basename($openid->identity);
			
			if(bccomp($id, "76561197960265728") > 0){
				//ID is valid steamid

				//API
				$data = file_get_contents("http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0001/?key=".APIKEY."&steamids=".$id);
				$profile = json_decode($data,true);

				//print_r($profile);

				//Profile Vars
				$username = $profile['response']['players']['player']['0']['personaname'];
				$avatar = $profile['response']['players']['player']['0']['avatarfull'];

				//Check for user
				$user = $sql->condQuery("SELECT * FROM `users` WHERE `steamid` = ?",array($id));
				if($user){
					$sql->condQuery("UPDATE  `scrap`.`users` SET  `username` =  ? , `avatar` = ? WHERE  `users`.`id` = ? LIMIT 1 ;",array($username,$avatar,$user[0]['id']));
					echo "Welcome back {$username}!";
				}else{
					//Create User
					$sql->condQuery("INSERT INTO `scrap`.`users` (`id`, `steamid`, `created`, `ip`, `group`, `username`, `avatar`) VALUES (NULL, ?, ?, ?, ?, ?, ?);",array($id,time(),$_SERVER['REMOTE_ADDR'],DEFAULT_GROUP,$username,$avatar));
					$user = $sql->condQuery("SELECT * FROM `users` WHERE `steamid` = ?",array($id));
					echo "<div class='login-msg'>Welcome {$username}! Enjoy your stay here at TF2 Scrap.</div>";
				}

				//Login
				$_SESSION['id'] = $user[0]['id'];
				$_SESSION['steamid'] = $id;

				//Redirect
				header("Location: /");


			}else{
				echo "<div class='login-error'>There was a problem logging you in.  Sorry about that.<br/>Error: SteamID was not in the correct format.</div>";
			}
		}else{
			echo "<div class='login-error'>There was a problem logging you in.  Sorry about that.<br/>Error: OpenID failed to validate your login.</div>";
		}
    }
} catch(ErrorException $e) {
	//Error
    $error = $e->getMessage();
    echo "<div class='login-error'>There was a problem logging you in.  Sorry about that.<br/>Error: {$error}</div>";

}
include "foot.php";
?>