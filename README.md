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
