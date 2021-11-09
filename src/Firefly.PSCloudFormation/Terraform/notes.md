# SSM Parameters

Reproduce like this

```
data "aws_ssm_parameter" "webserver-ami" {
  name = "/aws/service/ami-amazon-linux-latest/amzn2-ami-hvm-x86_64-gp2"
}
```

# CFN Init metadata diectly on EC2 instance

Do with provisioner block on `aws_instance`. Need to create a key pair that's shared with the terminal you're using
```
resource "aws_key_pair" "webserver-key" {
  key_name   = "webserver-key"
  public_key = file("~/.ssh/id_rsa.pub")
}
```

```
  provisioner "remote-exec" {
    inline = [
      "sudo yum -y install httpd && sudo systemctl start httpd",
      "echo '<h1><center>My Test Website With Help From Terraform Provisioner</center></h1>' > index.html",
      "sudo mv index.html /var/www/html/"
    ]
    connection {
      type        = "ssh"
      user        = "ec2-user"
      private_key = file("~/.ssh/id_rsa")
      host        = self.public_ip
    }
  }
```

# Other metadata on resources

```
resource "kubernetes_service" "tf-k8s-service" {
  metadata {
    name = "terraform-k8s-service"
    labels = {
      name = "tf-k8s-deploy"
    }
  }
  ...
```

