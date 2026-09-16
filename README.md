# FishingCat

Projeto Unity 6 com movimento em terceira pessoa e grappling.

## Controles

| Acao | Tecla |
| --- | --- |
| Mover | `WASD` ou setas |
| Pular | `Space` |
| Girar a camera | Segurar botao direito e mover o mouse |
| Mirar e disparar o grappling | Apontar com a camera e clicar/segurar botao esquerdo |
| Escalar uma parede presa pelo grappling | `W` para subir e `S` para descer |
| Soltar o grappling | Soltar o botao esquerdo |
| Arremessar a linha de pesca | `F` apontando para um peixe |
| Recolher a linha | Segurar `R` |
| Soltar a linha | `G` |

O grappling puxa o personagem ate o ponto atingido. Ao acertar uma superficie considerada parede, `W` e `S` controlam a escalada enquanto houver stamina.

## Grappling

Os valores padrao configurados no `FishingCatController` sao:

- Alcance: 30 m
- Velocidade de puxao: 25
- Cooldown apos um disparo valido: 3 s
- Stamina de escalada: 5 s
- Consumo de stamina: 1 por segundo
- Recuperacao de stamina: 1,5 por segundo quando o grappling nao esta ativo

## Configuracao no Unity

1. Adicione `FishingCatController` ao personagem que possui um `Rigidbody`.
2. Defina `Player Camera` ou deixe o campo vazio para usar a camera principal.
3. Em `Grapple Layer`, selecione as layers dos objetos que podem receber a corda.
4. Em `Ground Layer`, selecione a layer do chao.
5. Ajuste os valores da secao `Grappling` no Inspector conforme o tamanho e a dificuldade da fase.

A corda e criada automaticamente com um `LineRenderer` no personagem, caso ainda nao exista um.

## Pesca

1. Adicione o componente `FishingFish` a cada peixe e um collider no mesmo objeto ou em um filho.
2. Coloque os peixes na layer `Water` ou em outra layer selecionada em `Fishing Layer`.
3. Arraste o objeto `RodTip` para `Rod Tip` no `FishingCatController`.
4. Ajuste `Fishing Range`, `Fishing Line Length`, `Reel Speed` e `Max Line Tension` no Inspector.

Ao apertar `F`, a linha espera a fisgada. Depois que o peixe pega, segure `R` para recolher. Recolher continuamente cansa o peixe, mas aumenta a tensao; solte `R` para ele recuperar forca. Se a tensao atingir o limite, a linha arrebenta. Um peixe cansado e proximo pode ser capturado automaticamente.
